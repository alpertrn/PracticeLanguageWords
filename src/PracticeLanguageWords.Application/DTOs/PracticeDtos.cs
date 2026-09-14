using PracticeLanguageWords.Domain.Enums;

namespace PracticeLanguageWords.Application.DTOs;

public enum PracticeSource
{
    /// <summary>Kategori pratigi: kart bilgisiyle birlikte gosterilir, kullanici Kolay/Orta/Zor secer.</summary>
    Category = 0,

    /// <summary>Bilmediğim kelimeler quizi: anlam gizlidir, kullanici yazar + sesli soyler.</summary>
    UnknownOnly = 1
}

/// <summary>Bir ornek cumle ve varsa Turkce cevirisi. Kelimenin 0, 1 ya da 2 ornegi olabilir.</summary>
public record ExampleSentenceDto(string Sentence, string? Translation);

/// <summary>Kartin arka yuzu: anlam, hafiza ipucu ve ornek cumle(ler).</summary>
public record CardAnswerDto(
    string MeaningText,
    string? MemoryConnection,
    IReadOnlyList<ExampleSentenceDto> Examples);

/// <summary>
/// Kart verisi. Kategori pratiginde <see cref="Answer"/> doludur (her sey tek ekranda gorunur).
/// Quiz modunda null'dur: cevap sunucudan sizdirilmaz, ancak cevap gonderildikten sonra doner.
///
/// <see cref="PronunciationEnabled"/> yalnizca kelime kullanicinin "Bilmediğim Kelimeler"
/// listesindeyse true olur; Kolay/Orta kartlarda mikrofon hic gosterilmez.
/// </summary>
public record PracticeCardDto(
    int WordId,
    string CategoryBadge,
    string LanguageName,
    string SpeechCode,
    string TermText,
    string? TermRead,
    bool PronunciationEnabled,
    int MasteryStreak,
    int MasteryTarget,
    CardAnswerDto? Answer);

/// <summary>Kategori pratiginde Kolay/Orta/Hiç Bilmiyorum butonlarindan biri.</summary>
public record RateRequest(
    int WordId,
    DifficultyLevel Difficulty,
    int? CategoryId);

public record RateResultDto(
    StreakInfoDto Streak,
    bool ShowMilestoneCelebration,
    bool AddedToUnknownList);

/// <summary>Quiz modunda yazilan cevap.</summary>
public record AnswerRequest(
    int WordId,
    string AnswerText,
    int? LanguageId,
    // Tarayici konusma tanimayi destekliyor mu? Desteklemiyorsa telaffuz adimi atlanir.
    bool SpeechSupported);

public record AnswerResultDto(
    bool IsCorrect,
    CardAnswerDto Answer,
    StreakInfoDto Streak,
    bool ShowMilestoneCelebration,
    // Metin dogruydu; simdi kullanicinin kelimeyi sesli soylemesi bekleniyor.
    bool RequiresPronunciation,
    int MasteryStreak,
    int MasteryTarget,
    bool AskMasteryPrompt,
    bool NoMoreUnknownWords);

/// <summary>Tarayicidan gelen konusma tanima sonucu.</summary>
public record PronunciationRequest(
    int WordId,
    IReadOnlyList<string> Transcripts,
    PracticeSource Source,
    int? LanguageId,
    // Bu, kullanicinin bu kart icin izinli 3 ses hakkindan sonuncusu mu?
    // Yanlissa ve son hak degilse sayaç henuz kesinlesmez, kullanici tekrar dener.
    // Yanlis olup son haksa (ya da dogruysa her zaman) sonuc kesinlesir ve gun damgasi basilir.
    bool IsFinalAttempt = false);

public record PronunciationResultDto(
    bool IsCorrect,
    string BestTranscript,
    string ExpectedTerm,
    int MasteryStreak,
    int MasteryTarget,
    // Hedefe ulasildi: kullaniciya "artik kolay mi?" penceresi gosterilecek.
    bool AskMasteryPrompt,
    bool NoMoreUnknownWords);

/// <summary>"Artık kolay mı?" penceresinin cevabi.</summary>
public record MasteryDecisionRequest(
    int WordId,
    bool MarkAsEasy,
    int? LanguageId);

public record MasteryDecisionResultDto(
    bool RemovedFromUnknownList,
    bool NoMoreUnknownWords);

/// <summary>
/// Pratik ekrani acilmadan once yapilan kontrol: calisilacak kelime var mi,
/// baslik ne olacak, yoksa kullaniciya hangi mesajla ana sayfaya donulecek.
/// </summary>
public record PracticeContextDto(bool CanStart, string Title, string Subtitle, string? RedirectMessage);

/// <summary>
/// Teknik bir mikrofon sorunu (izin yok, cihaz yok, tarayici desteklemiyor, tekrarli sessizlik)
/// nedeniyle telaffuz adimi tamamlanamadiginda gonderilir. Basari/basarisizlik sayacina
/// DOKUNMAZ - sadece kelimenin bugun icin puanlandigini isaretler ki ayni gun tekrar cikmasin.
/// </summary>
public record PostponeUnknownWordRequest(int WordId);
