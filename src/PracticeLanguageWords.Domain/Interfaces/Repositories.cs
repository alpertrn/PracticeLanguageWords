using PracticeLanguageWords.Domain.Entities;
using PracticeLanguageWords.Domain.Models;

namespace PracticeLanguageWords.Domain.Interfaces;

// Repository arayuzleri Domain katmaninda tanimlanir, Infrastructure tarafinda implemente edilir.
// Bu, Dependency Inversion Principle'in uygulanmasidir: Application katmani somut EF Core'a degil,
// bu soyutlamalara baglidir. Her arayuz sadece ilgili ozelligi acar (Interface Segregation).

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);
    void Add(User user);

    /// <summary>
    /// "Son görülme" zaman damgasini gunceller (tek SQL UPDATE - entity yuklemeye gerek yok,
    /// bkz. IUserStreakLogRepository.IncrementCardCountAsync - ayni desen).
    /// </summary>
    Task TouchLastSeenAsync(int userId, DateTime utcNow, CancellationToken ct = default);
}

public interface ILanguageRepository
{
    Task<List<Language>> GetAllAsync(bool onlyActive, CancellationToken ct = default);
    Task<Language?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken ct = default);
    Task<bool> HasCategoriesAsync(int languageId, CancellationToken ct = default);
    void Add(Language language);
    void Remove(Language language);
}

public interface ICategoryGroupRepository
{
    Task<List<CategoryGroup>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryGroup?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> HasCategoriesAsync(int groupId, CancellationToken ct = default);
    void Add(CategoryGroup group);
    void Remove(CategoryGroup group);
}

public interface ICategoryRepository
{
    /// <summary>languageId verilirse sadece o dilin kategorileri doner.</summary>
    Task<List<CategoryWithCount>> GetAllWithWordCountAsync(int? languageId, CancellationToken ct = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> HasWordsAsync(int categoryId, CancellationToken ct = default);
    void Add(Category category);
    void Remove(Category category);
}

public interface IWordRepository
{
    Task<Word?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Word>> GetByCategoryIdAsync(int categoryId, CancellationToken ct = default);
    Task<List<Word>> SearchAsync(string? term, int? categoryId, int? languageId, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountSearchAsync(string? term, int? categoryId, int? languageId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int categoryId, string termText, int? excludeWordId, CancellationToken ct = default);

    /// <summary>Bir kategorideki tum kelimeleri, kullanicinin zorluk bilgisiyle birlikte (projeksiyon) doner.</summary>
    Task<List<WordCandidate>> GetCandidatesInCategoryAsync(int userId, int categoryId, CancellationToken ct = default);

    /// <summary>
    /// Kullanicinin "Zor" isaretledigi (IsUnknown) kelimeleri doner; dile gore filtrelenebilir.
    /// Bu takvim gununde (Turkiye saati) zaten puanlanmis kelimeler haric tutulur, boylece
    /// bir kelime ayni gun icinde ikinci kez karsimiza cikip listeden dusurulmez.
    /// </summary>
    Task<List<WordCandidate>> GetUnknownCandidatesAsync(int userId, int? languageId, DateOnly today, CancellationToken ct = default);

    void Add(Word word);
    void AddRange(IEnumerable<Word> words);
    void Remove(Word word);
}

public interface IUserWordProgressRepository
{
    Task<UserWordProgress?> GetAsync(int userId, int wordId, CancellationToken ct = default);

    /// <summary>
    /// Kaydi getirir, yoksa atomik olarak olusturur. Iki es zamanli istek (cift tiklama, ag
    /// tekrar denemesi, coklu sekme) ayni (UserId, WordId) icin ayni anda "yok" gorup ikisi
    /// de eklemeye calisirsa, kaybeden taraf composite PK ihlalini yakalar, kendi eklemesini
    /// geri alir ve kazananin kaydini okur - cagiran kod her zaman gecerli, DB'de var olan
    /// bir kayit alir. (bkz. IUserStreakLogRepository.TryCreateForTodayAsync - ayni desen.)
    /// </summary>
    Task<UserWordProgress> GetOrCreateAsync(int userId, int wordId, CancellationToken ct = default);

    Task<List<UserWordProgress>> GetUnknownAsync(int userId, int? languageId, CancellationToken ct = default);
    Task<int> CountUnknownAsync(int userId, int? languageId, CancellationToken ct = default);

    /// <summary>
    /// Bilinmeyenler icinde, bugun (Turkiye takvimi) HENUZ puanlanmamis kelime sayisi.
    /// Toplam bilinmeyen sayisi > 0 ama bu deger 0 ise, kullanici o gunku calismasini
    /// tamamlamis demektir - "yarin tekrar gel" mesaji bunun icin kullanilir.
    /// </summary>
    Task<int> CountUnknownAvailableTodayAsync(int userId, int? languageId, DateOnly today, CancellationToken ct = default);
    void Add(UserWordProgress progress);
    void Remove(UserWordProgress progress);
}

public interface IUserStreakRepository
{
    Task<UserStreak?> GetByUserIdAsync(int userId, CancellationToken ct = default);
    void Add(UserStreak streak);
}

public interface IUserStreakLogRepository
{
    Task<UserStreakLog?> GetForDateAsync(int userId, DateOnly date, CancellationToken ct = default);
    Task<List<UserStreakLog>> GetLastNDaysAsync(int userId, int days, DateOnly today, CancellationToken ct = default);

    /// <summary>
    /// O gune ait log satirini atomik olarak olusturmaya calisir (unique index: UserId+ActivityDate).
    /// Iki es zamanli istek ayni anda gelirse sadece biri true doner; digeri false alir ve
    /// streak sayacini tekrar artirmaz. Bu sekilde streak artisi cift tiklamaya/yarisa karsi guvenlidir.
    /// </summary>
    Task<bool> TryCreateForTodayAsync(int userId, DateOnly date, CancellationToken ct = default);

    /// <summary>Ayni gun icindeki sonraki kartlar icin CardCount sayacini 1 artirir.</summary>
    Task IncrementCardCountAsync(int userId, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// [weekStart, weekEnd] araligindaki CardCount toplamlarini kullanici bazinda azalan
    /// sirada doner (lider tablosu). O hafta hic aktivitesi olmayan kullanicilar listede
    /// yer almaz - "katilimci" olmayan biri siralamada gorunmemelidir.
    /// </summary>
    Task<List<LeaderboardRow>> GetWeeklyLeaderboardAsync(DateOnly weekStart, DateOnly weekEnd, CancellationToken ct = default);
}
