using PracticeLanguageWords.Domain.Enums;

namespace PracticeLanguageWords.Domain.Entities;

/// <summary>
/// Composite key: (UserId, WordId). Bir kullanicinin bir kelimeyle olan gecmisini tutar.
///
/// ONEMLI AYRIM:
///   Difficulty  -> kelimenin ne siklikta gosterilecegini belirler (agirlikli secim).
///   IsUnknown   -> kelimenin "Bilmediğim Kelimeler" listesinde olup olmadigini belirler.
/// Bu ikisi bagimsizdir: kategori pratiginde "Kolay" demek kelimeyi seyrek gosterir ama
/// listeden DUSURMEZ. Listeden dusurme yalnizca Bilmediğim Kelimeler menusunde olur.
/// </summary>
public class UserWordProgress
{
    public int UserId { get; set; }
    public int WordId { get; set; }

    public DifficultyLevel Difficulty { get; set; }
    public bool IsUnknown { get; set; }
    public int ReviewCount { get; set; }
    public DateTime LastReviewedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Bilmediğim Kelimeler quizinde FARKLI GUNLERDE kac kez hem anlamini dogru yazdi
    /// hem de dogru telaffuz etti (ayni gun icinde birden fazla kez sayilmaz - bkz.
    /// <see cref="LastMasteryAttemptDate"/>). Hedefe ulasinca kullaniciya "artik kolay mi?"
    /// sorulur. Herhangi bir gunku denemede basarisiz olursa (yanlis metin ya da 3 hakta da
    /// tutmayan telaffuz) sifirlanir.
    /// </summary>
    public int MasteryStreak { get; set; }

    /// <summary>
    /// Bilmediğim Kelimeler quizinde bu kelime icin son kez puanlanmis (basarili ya da
    /// basarisiz sonuclanmis) denemenin Turkiye takvim gunu. Ayni gun icinde bu kelime
    /// tekrar secilmez/puanlanmaz - boylece kullanici bir kelimeyi ayni oturumda ust uste
    /// gorup listeden dusurmez, degerlendirme birkac gune yayilir.
    /// </summary>
    public DateOnly? LastMasteryAttemptDate { get; set; }

    public int PronunciationAttempts { get; set; }
    public int PronunciationSuccesses { get; set; }
    public DateTime? LastPronunciationAt { get; set; }

    public User User { get; set; } = null!;
    public Word Word { get; set; } = null!;
}
