namespace PracticeLanguageWords.Application.Common;

/// <summary>
/// Sunucu hangi saat diliminde calisirsa calissin (UTC container, vs.) tum tarih
/// karsilastirmalari Turkiye yerel saatine (UTC+3) gore yapilir. Bu, gece 00:00-03:00
/// arasi streak yanlis sifirlanmasi uc durumunu (teknik sartname madde 5.2) cozer.
/// </summary>
public static class TurkeyClock
{
    private static readonly TimeZoneInfo TimeZone = ResolveTimeZone();

    private static TimeZoneInfo ResolveTimeZone()
    {
        try
        {
            // IANA kimligi; .NET 6+ hem Linux hem Windows'ta ICU uzerinden bunu cozer.
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows'ta ICU verisi eksikse Windows kimligine dus.
            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
    }

    public static DateOnly Today()
    {
        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, TimeZone);
        return DateOnly.FromDateTime(nowLocal);
    }

    /// <summary>Günlük bildirim zamanlamasi (saat kontrolu) icin - Today()'den farkli olarak saat bilgisini de tasir.</summary>
    public static DateTime Now()
    {
        var nowUtc = DateTime.UtcNow;
        return TimeZoneInfo.ConvertTimeFromUtc(nowUtc, TimeZone);
    }
}
