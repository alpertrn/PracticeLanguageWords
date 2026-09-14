using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PracticeLanguageWords.Application.Interfaces;
using WebPush;

namespace PracticeLanguageWords.Infrastructure.Notifications;

/// <summary>
/// Web Push API (VAPID) uzerinden bildirim gonderir - ucuncu parti bir servise
/// (Firebase, OneSignal vb.) bagimlilik yoktur; tarayicinin kendi push servisine
/// (Chrome icin FCM, Firefox icin Mozilla, vb.) dogrudan sifrelenmis mesaj gonderilir.
/// VAPID anahtarlari appsettings'te "WebPush" altinda tanimlanir (bkz. README).
/// </summary>
public class WebPushSender : IPushSender
{
    private readonly VapidDetails _vapidDetails;
    private readonly WebPushClient _client = new();
    private readonly ILogger<WebPushSender> _logger;

    public WebPushSender(IConfiguration configuration, ILogger<WebPushSender> logger)
    {
        var publicKey = configuration["WebPush:VapidPublicKey"]
            ?? throw new InvalidOperationException("'WebPush:VapidPublicKey' tanimli degil.");
        var privateKey = configuration["WebPush:VapidPrivateKey"]
            ?? throw new InvalidOperationException("'WebPush:VapidPrivateKey' tanimli degil.");
        var subject = configuration["WebPush:Subject"] ?? "mailto:admin@example.com";

        _vapidDetails = new VapidDetails(subject, publicKey, privateKey);
        _logger = logger;
    }

    public async Task<PushSendResult> SendAsync(
        string endpoint, string p256dh, string auth, string title, string body, string? url, CancellationToken ct = default)
    {
        var subscription = new PushSubscription(endpoint, p256dh, auth);
        var payload = JsonSerializer.Serialize(new { title, body, url = url ?? "/" });

        try
        {
            await _client.SendNotificationAsync(subscription, payload, _vapidDetails);
            return PushSendResult.Sent;
        }
        catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
        {
            // Abonelik artik gecerli degil (uygulama kaldirilmis, izin geri alinmis, cok eski).
            return PushSendResult.SubscriptionGone;
        }
        catch (WebPushException ex)
        {
            _logger.LogWarning(ex, "Push bildirimi gonderilemedi ({StatusCode}).", ex.StatusCode);
            return PushSendResult.Failed;
        }
    }
}
