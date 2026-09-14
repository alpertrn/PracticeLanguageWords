using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PracticeLanguageWords.Application.Common;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Infrastructure.Notifications;

/// <summary>
/// Uygulama surekli ayaktayken 15 dakikada bir saati kontrol eder; belirlenen saate
/// (varsayilan 20:00, Turkiye saati - appsettings "Notifications:HourOfDay" ile
/// degistirilebilir) gelindiginde gunluk bildirim kontrolunu (NotificationService)
/// bir kez calistirir.
///
/// ONEMLI: Bu, uygulama surecinin kendisi calisirken isleyen bir arka plan gorevidir.
/// Yalnizca Visual Studio'da debug icin acildiginda calisir; uygulama kapaninca durur.
/// Her gun otomatik bildirim gitmesi icin sitenin IIS/production ortaminda surekli
/// calisir durumda barindirilmasi gerekir - bkz. README "Bildirimler" bolumu.
/// </summary>
public class DailyNotificationBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DailyNotificationBackgroundService> _logger;
    private DateOnly? _lastRunDate;

    public DailyNotificationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<DailyNotificationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndRunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Günlük bildirim kontrolü sırasında hata oluştu.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Uygulama kapaniyor - dongu zaten sonlanacak.
            }
        }
    }

    private async Task CheckAndRunAsync(CancellationToken ct)
    {
        var nowLocal = TurkeyClock.Now();
        var today = DateOnly.FromDateTime(nowLocal);

        if (_lastRunDate == today)
        {
            return; // Bugun zaten calisti.
        }

        // GetValue<T>, Microsoft.Extensions.Configuration.Binder paketine bagli bir extension
        // metod - projede sadece Configuration.Abstractions referansi var, gereksiz paket
        // eklememek icin degeri elle okuyup parse ediyoruz.
        var hourOfDayText = _configuration["Notifications:HourOfDay"];
        var hourOfDay = int.TryParse(hourOfDayText, out var parsedHour) ? parsedHour : 20;

        if (nowLocal.Hour < hourOfDay)
        {
            return; // Henuz saati gelmedi.
        }

        _lastRunDate = today;

        using var scope = _scopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var sentCount = await notificationService.SendDailyNotificationsAsync(ct);

        _logger.LogInformation("Günlük bildirim kontrolü tamamlandı: {SentCount} kullanıcıya gönderildi.", sentCount);
    }
}
