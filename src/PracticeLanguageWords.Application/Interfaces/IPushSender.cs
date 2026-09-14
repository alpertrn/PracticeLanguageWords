namespace PracticeLanguageWords.Application.Interfaces;

public enum PushSendResult
{
    Sent,

    /// <summary>Tarayici/isletim sistemi bu aboneligi artik gecersiz sayiyor (410 Gone / 404) - satir silinmeli.</summary>
    SubscriptionGone,

    Failed
}

/// <summary>
/// Web Push gonderim detayini (VAPID imzalama, sifreleme) Application katmanindan gizler.
/// Implementasyon Infrastructure katmanindadir (SOLID - Dependency Inversion, bkz. IPasswordHasher).
/// </summary>
public interface IPushSender
{
    Task<PushSendResult> SendAsync(
        string endpoint,
        string p256dh,
        string auth,
        string title,
        string body,
        string? url,
        CancellationToken ct = default);
}
