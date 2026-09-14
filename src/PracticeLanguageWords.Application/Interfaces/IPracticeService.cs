using PracticeLanguageWords.Application.DTOs;

namespace PracticeLanguageWords.Application.Interfaces;

public interface IPracticeService
{
    Task<PracticeContextDto> GetContextAsync(int userId, PracticeSource source, int? categoryId, int? languageId, CancellationToken ct = default);

    Task<PracticeCardDto?> GetNextCardAsync(
        int userId,
        PracticeSource source,
        int? categoryId,
        int? languageId,
        IReadOnlyCollection<int> recentlyShownWordIds,
        CancellationToken ct = default);

    /// <summary>Kategori pratigi: Kolay/Orta/Hiç Bilmiyorum degerlendirmesini kaydeder.</summary>
    Task<RateResultDto> RateAsync(int userId, RateRequest request, CancellationToken ct = default);

    /// <summary>Quiz modu 1. adim: yazilan anlami degerlendirir ve kartin arka yuzunu doner.</summary>
    Task<AnswerResultDto> SubmitAnswerAsync(int userId, AnswerRequest request, CancellationToken ct = default);

    /// <summary>Quiz modu 2. adim (ve kategori pratiginde opsiyonel): sesli telaffuz denemesi.</summary>
    Task<PronunciationResultDto> SubmitPronunciationAsync(int userId, PronunciationRequest request, CancellationToken ct = default);

    /// <summary>"Artık kolay mı?" penceresinin cevabini uygular (listeden dusurmenin tek otomatik yolu).</summary>
    Task<MasteryDecisionResultDto> ApplyMasteryDecisionAsync(int userId, MasteryDecisionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Bilmediğim Kelimeler havuzu bosken (source = UnknownOnly) gosterilecek mesaji secer:
    /// hic bilinmeyen kelime kalmadiysa kutlama mesaji, bugunku pay tukendiyse "yarin tekrar gel" mesaji.
    /// </summary>
    Task<string> GetUnknownFinishedMessageAsync(int userId, int? languageId, CancellationToken ct = default);

    /// <summary>
    /// Teknik bir mikrofon sorunu yuzunden telaffuz adimi tamamlanamadiginda cagrilir.
    /// Basari sayacina dokunmaz, sadece kelimeyi "bugun icin puanlandi" olarak isaretler.
    /// </summary>
    Task PostponeUnknownWordAsync(int userId, PostponeUnknownWordRequest request, CancellationToken ct = default);
}

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(int userId, int? languageId, CancellationToken ct = default);
}

public interface IMyWordsService
{
    Task<MyWordsDto> GetUnknownWordsAsync(int userId, int? languageId, CancellationToken ct = default);

    /// <summary>Kelimeyi "bilinmeyenler" listesinden cikarir (Kolay olarak isaretler).</summary>
    Task RemoveFromUnknownAsync(int userId, int wordId, CancellationToken ct = default);
}

public interface ILanguageService
{
    Task<IReadOnlyList<LanguageSummaryDto>> GetActiveLanguagesAsync(CancellationToken ct = default);

    /// <summary>URL'deki dil kodunu ("en", "de") aktif bir dile cevirir; bulunamazsa null.</summary>
    Task<LanguageSummaryDto?> GetByCodeAsync(string? code, CancellationToken ct = default);

    /// <summary>Varsayilan (ilk siradaki aktif) dil; hic dil yoksa null.</summary>
    Task<LanguageSummaryDto?> GetDefaultAsync(CancellationToken ct = default);

    /// <summary>Istenen dil gecerli degilse ilk aktif dile duser; hic dil yoksa 0 doner.</summary>
    Task<int> ResolveLanguageIdAsync(int? requestedLanguageId, CancellationToken ct = default);
}
