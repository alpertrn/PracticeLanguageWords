namespace PracticeLanguageWords.Domain.Entities;

/// <summary>
/// Gunluk aktivite kaydi. (UserId, ActivityDate) uzerinde unique index vardir;
/// bu sayede ayni gun icin streak artisi idempotent hale gelir (cift tikla / cift istek koruma).
/// </summary>
public class UserStreakLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateOnly ActivityDate { get; set; }
    public int CardCount { get; set; }

    public User User { get; set; } = null!;
}
