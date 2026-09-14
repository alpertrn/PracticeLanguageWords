using PracticeLanguageWords.Application.Common;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Domain.Entities;
using PracticeLanguageWords.Domain.Enums;
using PracticeLanguageWords.Domain.Interfaces;

namespace PracticeLanguageWords.Application.Services;

/// <summary>
/// Iki calisma modu ve iki ayri kavram yonetir.
///
/// MODLAR
///  1) Kategori pratigi (PracticeSource.Category)
///     Kart tum bilgisiyle gosterilir. Kullanici Kolay / Orta / Hiç Bilmiyorum secer.
///     Kelime zaten "bilinmeyen" listesindeyse ek olarak telaffuz denemesi de yapabilir.
///  2) Bilmediğim kelimeler quizi (PracticeSource.UnknownOnly)
///     Anlam gizlidir. Kullanici once anlamini yazar (TEK HAK), dogruysa kelimeyi sesli
///     soyler (3 HAK - ucuncude de tutmazsa "bilmedi" sayilir).
///
/// KAVRAMLAR
///  Difficulty -> gosterilme sikligi (agirlikli secim)
///  IsUnknown  -> listede olup olmadigi
/// Bunlar bagimsizdir. LISTEDEN DUSURME YALNIZCA QUIZ MODUNDA OLUR ve ancak kullanici
/// FARKLI GUNLERDE <see cref="MasteryTarget"/> kez basarili olup "artık kolay" onayi
/// verirse gerceklesir. Ayni kelime ayni takvim gunu icinde birden fazla kez
/// puanlanmaz (bkz. UserWordProgress.LastMasteryAttemptDate) - yani en az
/// (MasteryTarget - 1) gun boyunca dagilmis olmadan bir kelime asla dusmez.
/// Kategori pratiginde dogru cevap/telaffuz kelimeyi listeden dusurmez, sadece sikligini azaltir.
/// </summary>
public class PracticeService : IPracticeService
{
    /// <summary>Listeden dusurme onerisi icin gereken, FARKLI GUNLERDE elde edilmis basari sayisi.</summary>
    public const int MasteryTarget = 4;

    private readonly IUnitOfWork _uow;
    private readonly IWordSelectionService _wordSelection;
    private readonly IAnswerEvaluator _answerEvaluator;
    private readonly IPronunciationEvaluator _pronunciationEvaluator;
    private readonly IStreakService _streakService;

    public PracticeService(
        IUnitOfWork uow,
        IWordSelectionService wordSelection,
        IAnswerEvaluator answerEvaluator,
        IPronunciationEvaluator pronunciationEvaluator,
        IStreakService streakService)
    {
        _uow = uow;
        _wordSelection = wordSelection;
        _answerEvaluator = answerEvaluator;
        _pronunciationEvaluator = pronunciationEvaluator;
        _streakService = streakService;
    }

    public async Task<PracticeContextDto> GetContextAsync(
        int userId,
        PracticeSource source,
        int? categoryId,
        int? languageId,
        CancellationToken ct = default)
    {
        if (source == PracticeSource.UnknownOnly)
        {
            var unknownCount = await _uow.WordProgresses.CountUnknownAsync(userId, languageId, ct);

            if (unknownCount == 0)
            {
                return new PracticeContextDto(false, string.Empty, string.Empty, "Tebrikler! Bilmediğin kelime kalmadı.");
            }

            var availableToday = await _uow.WordProgresses.CountUnknownAvailableTodayAsync(userId, languageId, TurkeyClock.Today(), ct);

            return availableToday == 0
                ? new PracticeContextDto(false, string.Empty, string.Empty, await GetUnknownFinishedMessageAsync(userId, languageId, ct))
                : new PracticeContextDto(true, "Bilmediğim Kelimeler", $"{unknownCount} kelime · anlamını yaz, sonra sesli söyle", null);
        }

        if (categoryId is null)
        {
            throw new BusinessRuleException("Kategori seçilmeden pratik başlatılamaz.");
        }

        var category = await _uow.Categories.GetByIdAsync(categoryId.Value, ct)
            ?? throw new NotFoundException("Kategori bulunamadı.");

        var hasWords = await _uow.Categories.HasWordsAsync(category.Id, ct);

        return hasWords
            ? new PracticeContextDto(true, category.Name, "Kartı incele, zorluk derecesini seç", null)
            : new PracticeContextDto(false, category.Name, string.Empty, $"'{category.Name}' kategorisinde henüz kelime yok.");
    }

    public async Task<PracticeCardDto?> GetNextCardAsync(
        int userId,
        PracticeSource source,
        int? categoryId,
        int? languageId,
        IReadOnlyCollection<int> recentlyShownWordIds,
        CancellationToken ct = default)
    {
        Word? word;

        if (source == PracticeSource.UnknownOnly)
        {
            word = await _wordSelection.SelectFromUnknownAsync(userId, languageId, recentlyShownWordIds, ct);
        }
        else
        {
            if (categoryId is null)
            {
                throw new BusinessRuleException("Kategori seçilmeden pratik başlatılamaz.");
            }

            word = await _wordSelection.SelectFromCategoryAsync(userId, categoryId.Value, recentlyShownWordIds, ct);
        }

        if (word is null)
        {
            return null; // havuz bos
        }

        var progress = await _uow.WordProgresses.GetAsync(userId, word.Id, ct);

        // Mikrofon yalnizca "bilinmeyen" isaretli kelimelerde acilir.
        // Kolay/Orta kartlarda ve hic gorulmemis kelimelerde telaffuz istenmez.
        var pronunciationEnabled = progress?.IsUnknown == true;

        // Kategori pratiginde arka yuz de gonderilir (tek ekran, her sey gorunur).
        // Quiz modunda GONDERILMEZ: cevap istemciye sizdirilmaz.
        var answer = source == PracticeSource.Category ? BuildAnswer(word) : null;

        return new PracticeCardDto(
            word.Id,
            word.Category.Name,
            word.Category.Language.Name,
            word.Category.Language.SpeechCode,
            word.TermText,
            word.TermRead,
            pronunciationEnabled,
            progress?.MasteryStreak ?? 0,
            MasteryTarget,
            answer);
    }

    /// <summary>
    /// Kategori pratigi. Difficulty her zaman guncellenir (gosterilme sikligi).
    /// IsUnknown yalnizca "Hiç Bilmiyorum" ile TRUE yapilir; Kolay/Orta bu bayraga DOKUNMAZ,
    /// yani kelime listedeyse listede kalir - sadece daha seyrek gosterilir.
    /// </summary>
    public async Task<RateResultDto> RateAsync(int userId, RateRequest request, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(request.Difficulty))
        {
            throw new BusinessRuleException("Geçersiz zorluk derecesi.");
        }

        var word = await _uow.Words.GetByIdAsync(request.WordId, ct)
            ?? throw new NotFoundException("Kelime bulunamadı.");

        var progress = await GetOrCreateProgressAsync(userId, word.Id, ct);
        var wasUnknown = progress.IsUnknown;

        progress.Difficulty = request.Difficulty;

        if (request.Difficulty == DifficultyLevel.Hard && !wasUnknown)
        {
            progress.IsUnknown = true;
            progress.MasteryStreak = 0;
        }

        progress.ReviewCount += 1;
        progress.LastReviewedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);

        var (streak, milestoneReached) = await _streakService.RegisterActivityAsync(userId, ct);

        return new RateResultDto(
            streak,
            milestoneReached,
            AddedToUnknownList: request.Difficulty == DifficultyLevel.Hard && !wasUnknown);
    }

    /// <summary>
    /// Quiz modu - 1. adim: yazilan anlam.
    /// Dogruysa kelime listeden DUSMEZ; telaffuz adimina gecilir.
    /// Yanlissa zorluk Hard'a cekilir ve ust uste basari sayaci sifirlanir.
    /// </summary>
    public async Task<AnswerResultDto> SubmitAnswerAsync(int userId, AnswerRequest request, CancellationToken ct = default)
    {
        var word = await _uow.Words.GetByIdAsync(request.WordId, ct)
            ?? throw new NotFoundException("Kelime bulunamadı.");

        var progress = await GetOrCreateProgressAsync(userId, word.Id, ct);
        var isCorrect = _answerEvaluator.IsCorrect(request.AnswerText, word.GetAcceptedMeanings());

        progress.ReviewCount += 1;
        progress.LastReviewedAt = DateTime.UtcNow;

        var requiresPronunciation = false;
        var askMasteryPrompt = false;

        if (!isCorrect)
        {
            // Metin icin tek hak var: yanlissa bu gunku deneme basarisiz sonuclanir.
            progress.Difficulty = DifficultyLevel.Hard;
            progress.IsUnknown = true;                       // listede kalir
            progress.MasteryStreak = 0;                      // basari zinciri bozuldu
            progress.LastMasteryAttemptDate = TurkeyClock.Today(); // bugun icin puanlandi, tekrar cikmaz
        }
        else if (request.SpeechSupported)
        {
            // Metin dogru: simdi sesli telaffuz bekleniyor (3 hak). Sayac ve gun damgasi
            // telaffuz sonucu kesinlesince islenecek (bkz. SubmitPronunciationAsync).
            requiresPronunciation = true;
        }
        else
        {
            // Tarayici mikrofonu/konusma tanimayi desteklemiyor: kullanici burada tikanmasin,
            // sadece metin dogrulugu ile ilerlesin. Bu da bugunku puanlanmis deneme sayilir.
            progress.MasteryStreak += 1;
            progress.LastMasteryAttemptDate = TurkeyClock.Today();
            askMasteryPrompt = progress.MasteryStreak >= MasteryTarget;
        }

        await _uow.SaveChangesAsync(ct);

        var (streak, milestoneReached) = await _streakService.RegisterActivityAsync(userId, ct);
        var noMoreUnknownWords = await _uow.WordProgresses.CountUnknownAsync(userId, request.LanguageId, ct) == 0;

        return new AnswerResultDto(
            isCorrect,
            BuildAnswer(word),
            streak,
            milestoneReached,
            requiresPronunciation,
            progress.MasteryStreak,
            MasteryTarget,
            askMasteryPrompt,
            noMoreUnknownWords);
    }

    /// <summary>
    /// Telaffuz denemesi. Her iki modda da istatistik (PronunciationAttempts/Successes) olarak
    /// kaydedilir, ama basari sayaci ve "artık kolay mı?" sorusu YALNIZCA quiz modunda isler.
    /// Kategori pratigindeki telaffuz sadece geri bildirimdir; listeyi etkilemez.
    ///
    /// Quiz modunda ses icin 3 hak vardir (<see cref="PronunciationRequest.IsFinalAttempt"/>).
    /// Dogru telaffuz HER ZAMAN aninda kesinlesir (1., 2. ya da 3. hakta olsun fark etmez).
    /// Yanlis telaffuz ise yalnizca SON hakta (3.) kesinlesir ve "bilmedi" sayilir; ilk iki
    /// yanlis denemede sayaç bozulmaz, kullanici sessizce tekrar dener.
    /// </summary>
    public async Task<PronunciationResultDto> SubmitPronunciationAsync(int userId, PronunciationRequest request, CancellationToken ct = default)
    {
        var word = await _uow.Words.GetByIdAsync(request.WordId, ct)
            ?? throw new NotFoundException("Kelime bulunamadı.");

        var progress = await GetOrCreateProgressAsync(userId, word.Id, ct);
        var judgement = _pronunciationEvaluator.Judge(request.Transcripts, word.TermText);

        progress.PronunciationAttempts += 1;

        if (judgement.IsCorrect)
        {
            progress.PronunciationSuccesses += 1;
            progress.LastPronunciationAt = DateTime.UtcNow;
        }

        var askMasteryPrompt = false;

        // Denemenin kesinlesip kesinlesmedigi: dogruysa her zaman, yanlissa yalnizca son haktaysa.
        var isDecisive = judgement.IsCorrect || request.IsFinalAttempt;

        if (request.Source == PracticeSource.UnknownOnly && isDecisive)
        {
            if (judgement.IsCorrect)
            {
                progress.MasteryStreak += 1;
                askMasteryPrompt = progress.MasteryStreak >= MasteryTarget;
            }
            else
            {
                progress.MasteryStreak = 0;
            }

            // Bugun icin puanlandi: bu kelime bugun tekrar karsimiza cikmaz.
            progress.LastMasteryAttemptDate = TurkeyClock.Today();
        }

        await _uow.SaveChangesAsync(ct);

        var noMoreUnknownWords = request.Source == PracticeSource.UnknownOnly
            && await _uow.WordProgresses.CountUnknownAsync(userId, request.LanguageId, ct) == 0;

        return new PronunciationResultDto(
            judgement.IsCorrect,
            judgement.BestTranscript,
            word.TermText,
            progress.MasteryStreak,
            MasteryTarget,
            askMasteryPrompt,
            noMoreUnknownWords);
    }

    /// <summary>
    /// "Artık kolay mı?" penceresinin cevabi. Listeden dusurmenin tek otomatik yolu burasidir.
    /// Hayir denirse kelime listede kalir ve sayac sifirlanir (yeniden <see cref="MasteryTarget"/>
    /// farkli gunde basari birikmeli).
    /// </summary>
    public async Task<MasteryDecisionResultDto> ApplyMasteryDecisionAsync(int userId, MasteryDecisionRequest request, CancellationToken ct = default)
    {
        var progress = await _uow.WordProgresses.GetAsync(userId, request.WordId, ct)
            ?? throw new NotFoundException("Kelime kaydı bulunamadı.");

        if (request.MarkAsEasy)
        {
            progress.IsUnknown = false;
            progress.Difficulty = DifficultyLevel.Easy;
        }

        progress.MasteryStreak = 0;
        await _uow.SaveChangesAsync(ct);

        var noMoreUnknownWords = await _uow.WordProgresses.CountUnknownAsync(userId, request.LanguageId, ct) == 0;

        return new MasteryDecisionResultDto(request.MarkAsEasy, noMoreUnknownWords);
    }

    /// <inheritdoc />
    public async Task<string> GetUnknownFinishedMessageAsync(int userId, int? languageId, CancellationToken ct = default)
    {
        var totalUnknown = await _uow.WordProgresses.CountUnknownAsync(userId, languageId, ct);

        return totalUnknown == 0
            ? "Tebrikler! Bilmediğin kelime kalmadı."
            : "Bugün için bilmediğin kelimeleri tamamladın. Kalan kelimeler birkaç gün içinde tekrar karşına çıkacak, yarın tekrar gel! 👋";
    }

    /// <inheritdoc />
    public async Task PostponeUnknownWordAsync(int userId, PostponeUnknownWordRequest request, CancellationToken ct = default)
    {
        var progress = await _uow.WordProgresses.GetAsync(userId, request.WordId, ct);

        if (progress is null)
        {
            return; // kayit yoksa yapacak bir sey yok, sessizce cik
        }

        // Sayaca DOKUNMUYORUZ: teknik bir mikrofon sorunu kullanicinin bilgisi hakkinda
        // bir sey soylemez. Sadece bugun icin bu kelimeyi "gorundu" olarak isaretliyoruz
        // ki ayni gun tekrar karsimiza cikmasin.
        progress.LastMasteryAttemptDate = TurkeyClock.Today();
        await _uow.SaveChangesAsync(ct);
    }

    private static CardAnswerDto BuildAnswer(Word word)
    {
        var examples = new List<ExampleSentenceDto>();

        if (!string.IsNullOrWhiteSpace(word.ExampleSentence))
        {
            examples.Add(new ExampleSentenceDto(word.ExampleSentence, word.ExampleSentenceMeaning));
        }

        if (!string.IsNullOrWhiteSpace(word.ExampleSentence2))
        {
            examples.Add(new ExampleSentenceDto(word.ExampleSentence2, word.ExampleSentenceMeaning2));
        }

        return new CardAnswerDto(word.MeaningText, word.MemoryConnection, examples);
    }

    /// <summary>
    /// Kaydi getirir, yoksa atomik olarak olusturur (bkz. IUserWordProgressRepository.GetOrCreateAsync -
    /// EF Core detaylari, StreakService.TryCreateForTodayAsync ile ayni yaris-durumu deseninde,
    /// Infrastructure katmaninda ele alinir; Application bunu bilmez).
    /// </summary>
    private Task<UserWordProgress> GetOrCreateProgressAsync(int userId, int wordId, CancellationToken ct) =>
        _uow.WordProgresses.GetOrCreateAsync(userId, wordId, ct);
}
