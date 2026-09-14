using PracticeLanguageWords.Application.Common;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Domain.Entities;
using PracticeLanguageWords.Domain.Interfaces;

namespace PracticeLanguageWords.Application.Services;

/// <summary>
/// Gunluk push bildirim kurallari:
/// 1) Bugun zaten bir kart cozulduyse hic bildirim gonderilmez (rahatsiz etmeye gerek yok).
/// 2) Seri "tehlikede"yse (dun aktifti, bugun henuz degil) - HER GUN hatirlatma gonderilir
///    ("serini bozma"). Ayrica bir siniflama yok, cunku zaten gunde en fazla 1 kez calisir.
/// 3) Seri olu/yoksa (2+ gundur aktivite yok) ve en az 3 gundur hic bildirim gitmediyse -
///    tesvik edici bir mesaj gonderilir. Boylece "az aktif" kullanicilar en fazla 3-4 gunde
///    bir rahatsiz edilir, her gun degil.
/// 4) Tam ortadaki durum (dun degil ama 2 gun once aktifti) icin bilerek sessiz kalinir -
///    seriyi az once kaybetmis birine hemen ertesi gun tekrar bildirim atmak can sikici olabilir.
/// </summary>
public class NotificationService : INotificationService
{
    private const int InactivityThresholdDays = 3;
    private const int MinDaysBetweenMotivationalPushes = 3;

    private static readonly string[] MotivationalMessages =
    {
        "Seni özledik! Birkaç kelimeye ne dersin? 5 dakikan yeter 💪",
        "Yeni kelimeler seni bekliyor. Hadi bir göz at! 📚",
        "Küçük bir alışkanlık büyük fark yaratır — bugün birkaç kelime çalışmaya ne dersin?",
        "Bir süredir görünmüyorsun. Kaldığın yerden devam edelim mi?"
    };

    private readonly IUnitOfWork _uow;
    private readonly IPushSender _pushSender;

    public NotificationService(IUnitOfWork uow, IPushSender pushSender)
    {
        _uow = uow;
        _pushSender = pushSender;
    }

    public async Task SubscribeAsync(int userId, PushSubscriptionRequest request, CancellationToken ct = default)
    {
        var existing = await _uow.PushSubscriptions.GetByEndpointAsync(request.Endpoint, ct);

        if (existing is not null)
        {
            // Ayni tarayici/cihaz farkli bir hesapla da abone olmus olabilir (paylasilan
            // bilgisayar, cikis-giris) - satiri guncelleriz, kopya olusturmayiz.
            existing.UserId = userId;
            existing.P256dh = request.P256dh;
            existing.Auth = request.Auth;
        }
        else
        {
            _uow.PushSubscriptions.Add(new PushSubscription
            {
                UserId = userId,
                Endpoint = request.Endpoint,
                P256dh = request.P256dh,
                Auth = request.Auth,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _uow.SaveChangesAsync(ct);
    }

    public async Task UnsubscribeAsync(int userId, string endpoint, CancellationToken ct = default)
    {
        var existing = await _uow.PushSubscriptions.GetByEndpointAsync(endpoint, ct);

        if (existing is not null && existing.UserId == userId)
        {
            _uow.PushSubscriptions.Remove(existing);
            await _uow.SaveChangesAsync(ct);
        }
    }

    public async Task<int> SendDailyNotificationsAsync(CancellationToken ct = default)
    {
        var today = TurkeyClock.Today();
        var candidates = await _uow.Users.GetNotificationCandidatesAsync(ct);
        var sentCount = 0;

        foreach (var user in candidates)
        {
            ct.ThrowIfCancellationRequested();

            // Guvenlik agi: is servisi bir gunde birden fazla kez tetiklenirse (ornegin
            // uygulama yeniden baslarsa) ayni gun icinde ikinci kez bildirim gitmesin.
            if (user.LastNotificationDate == today)
            {
                continue;
            }

            var doneToday = await _uow.StreakLogs.GetForDateAsync(user.Id, today, ct) is not null;
            if (doneToday)
            {
                continue;
            }

            var lastActive = user.UserStreak?.LastActiveDate;
            var daysSinceActive = lastActive is null
                ? int.MaxValue
                : today.DayNumber - lastActive.Value.DayNumber;

            string title;
            string body;

            if (daysSinceActive == 1 && (user.UserStreak?.CurrentStreak ?? 0) >= 1)
            {
                // Dun aktifti, seri hala ayakta ama bugun henuz kart cozulmedi - riskli.
                var streak = user.UserStreak!.CurrentStreak;
                title = "Serini bozma! 🔥";
                body = $"{streak} günlük serin bugün tehlikede. Birkaç kelime çözüp devam ettir.";
            }
            else if (daysSinceActive >= InactivityThresholdDays)
            {
                var daysSinceLastNotification = user.LastNotificationDate is null
                    ? int.MaxValue
                    : today.DayNumber - user.LastNotificationDate.Value.DayNumber;

                if (daysSinceLastNotification < MinDaysBetweenMotivationalPushes)
                {
                    continue;
                }

                title = "PracticeLanguageWords";
                body = await BuildMotivationalMessageAsync(user.Id, ct);
            }
            else
            {
                // Ara donem (ornegin tam 2 gundur aktivite yok): bilerek sessiz kalinir.
                continue;
            }

            var anySent = await SendToAllSubscriptionsAsync(user, title, body, ct);

            if (anySent)
            {
                user.LastNotificationDate = today;
                sentCount++;
            }

            // Gecersiz (SubscriptionGone) abonelikler silinmis olabilir - anySent false
            // olsa bile bu silmeler kaydedilmeli, yoksa ayni olu abonelige her gun
            // tekrar gonderim denenir.
            await _uow.SaveChangesAsync(ct);
        }

        return sentCount;
    }

    /// <summary>
    /// Az aktif kullaniciya gidecek tesvik mesajini secer. Sabit genel mesajlara ek olarak,
    /// kullanicinin "Bilmedigim Kelimeler" listesinde kelime varsa (tum diller toplami),
    /// bu sayiyi anan kisisellestirilmis bir mesaj da havuza eklenir - boylece bazen genel,
    /// bazen "X kelimeni bekliyor" tarzinda somut bir mesaj rastgele secilmis olur.
    /// </summary>
    private async Task<string> BuildMotivationalMessageAsync(int userId, CancellationToken ct)
    {
        var unknownCount = await _uow.WordProgresses.CountUnknownAsync(userId, languageId: null, ct);

        var candidates = new List<string>(MotivationalMessages);

        if (unknownCount > 0)
        {
            candidates.Add(unknownCount == 1
                ? "Bilmediğin 1 kelime seni bekliyor. Şimdi çalışmaya ne dersin? 📚"
                : $"Bilmediğin {unknownCount} kelime var. Birkaçına şimdi çalışmaya ne dersin? 📚");
        }

        return candidates[Random.Shared.Next(candidates.Count)];
    }

    private async Task<bool> SendToAllSubscriptionsAsync(User user, string title, string body, CancellationToken ct)
    {
        var anySent = false;

        foreach (var subscription in user.PushSubscriptions.ToList())
        {
            var result = await _pushSender.SendAsync(
                subscription.Endpoint, subscription.P256dh, subscription.Auth, title, body, "/", ct);

            switch (result)
            {
                case PushSendResult.Sent:
                    anySent = true;
                    break;
                case PushSendResult.SubscriptionGone:
                    // Tarayici/isletim sistemi bu aboneligi artik tanimiyor (uygulama kaldirilmis,
                    // izin geri alinmis, cok eski abonelik) - kayitli tutmanin anlami yok.
                    _uow.PushSubscriptions.Remove(subscription);
                    break;
                case PushSendResult.Failed:
                default:
                    break;
            }
        }

        return anySent;
    }
}
