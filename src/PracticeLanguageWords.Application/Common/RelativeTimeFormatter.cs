namespace PracticeLanguageWords.Application.Common;

/// <summary>
/// Lider tablosundaki "Son görülme" sütunu için kısa Türkçe göreli zaman metni
/// üretir (1dk, 3sa, 7g, 3ay, 2yıl gibi). Zaman dilimi burada önemli değildir:
/// hem "şimdi" hem de kaydedilen zaman UTC olarak tutulur, sadece aradaki fark hesaplanır.
/// </summary>
public static class RelativeTimeFormatter
{
    public static string ToShortTurkish(DateTime? pastUtc, DateTime? nowUtc = null)
    {
        if (pastUtc is null)
        {
            return "—";
        }

        var now = nowUtc ?? DateTime.UtcNow;
        var elapsed = now - pastUtc.Value;

        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        if (elapsed.TotalMinutes < 1)
        {
            return "şimdi";
        }

        if (elapsed.TotalMinutes < 60)
        {
            return $"{(int)elapsed.TotalMinutes}dk";
        }

        if (elapsed.TotalHours < 24)
        {
            return $"{(int)elapsed.TotalHours}sa";
        }

        if (elapsed.TotalDays < 30)
        {
            return $"{(int)elapsed.TotalDays}g";
        }

        if (elapsed.TotalDays < 365)
        {
            return $"{Math.Max(1, (int)(elapsed.TotalDays / 30))}ay";
        }

        return $"{Math.Max(1, (int)(elapsed.TotalDays / 365))}yıl";
    }
}
