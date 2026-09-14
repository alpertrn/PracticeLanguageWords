namespace PracticeLanguageWords.Domain.Entities;

/// <summary>
/// Kullanici basina 1-1 ozet seri bilgisi (PK = FK = UserId).
/// </summary>
public class UserStreak
{
    public int UserId { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateOnly? LastActiveDate { get; set; }

    public User User { get; set; } = null!;
}
