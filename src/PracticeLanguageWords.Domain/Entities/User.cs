using PracticeLanguageWords.Domain.Enums;

namespace PracticeLanguageWords.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // v6.0: lider tablosunda kullanici adi yerine gercek ismin gosterilebilmesi icin eklendi.
    // Mevcut kullanicilarda (migration oncesi kayitlar) bos gelebilir; goruntulemede
    // bos ise Username'e geri dusulur (bkz. LeaderboardService.BuildRanked).
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Lider tablosundaki "Son görülme" sutunu icin: kullanici bir kart uzerinde islem
    // yaptiginda (kategori pratiginde Kolay/Orta/Hiç Bilmiyorum ya da quiz cevabi) guncellenir
    // - bkz. StreakService.RegisterActivityAsync. UTC olarak tutulur (DateOnly degil, çünkü
    // "1sa. önce" gibi saat hassasiyetli bir gösterim gerekiyor).
    public DateTime? LastSeenAt { get; set; }

    public UserStreak? UserStreak { get; set; }
    public ICollection<UserWordProgress> WordProgresses { get; set; } = new List<UserWordProgress>();
    public ICollection<UserStreakLog> StreakLogs { get; set; } = new List<UserStreakLog>();
}
