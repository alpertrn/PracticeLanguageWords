using PracticeLanguageWords.Application.DTOs;

namespace PracticeLanguageWords.Application.Interfaces;

public interface INotificationService
{
    Task SubscribeAsync(int userId, PushSubscriptionRequest request, CancellationToken ct = default);
    Task UnsubscribeAsync(int userId, string endpoint, CancellationToken ct = default);

    /// <summary>
    /// Gunluk bildirim kontrolunu calistirir (bkz. DailyNotificationBackgroundService).
    /// Geriye kac kullaniciya bildirim gonderildigini doner (loglama icin).
    /// </summary>
    Task<int> SendDailyNotificationsAsync(CancellationToken ct = default);
}
