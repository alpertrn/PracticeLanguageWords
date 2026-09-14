namespace PracticeLanguageWords.Domain.Entities;

/// <summary>
/// Tarayicinin Push API aboneligi (bir kullanicinin birden fazla cihazi/tarayicisi olabilir,
/// her biri ayri bir satirdir). Endpoint + P256dh + Auth, tarayicinin push servisine (Chrome
/// icin FCM, Firefox icin Mozilla push, vb.) sifrelenmis bildirim gondermek icin yeterlidir -
/// VAPID anahtarlariyla birlikte kullanilir (bkz. WebPushSender).
/// </summary>
public class PushSubscription
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
