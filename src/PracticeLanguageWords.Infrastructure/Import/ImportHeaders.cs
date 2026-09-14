namespace PracticeLanguageWords.Infrastructure.Import;

/// <summary>
/// Kolon basligi esleme tablosu. Yeni (dilden bagimsiz) adlarin yani sira
/// eski Ingilizce-odakli adlar da kabul edilir; boylece onceden hazirlanmis
/// kelime listeleri degistirilmeden yuklenebilir.
/// </summary>
internal static class ImportHeaders
{
    public static readonly string[] Term = { "termtext", "englishtext", "kelime", "word" };
    public static readonly string[] Meaning = { "meaningtext", "turkishtext", "anlam", "meaning" };
    public static readonly string[] Read = { "termread", "turkishread", "okunus", "okunuş" };
    public static readonly string[] Memory = { "memoryconnection", "hafizaipucu", "hafızaipucu" };
    public static readonly string[] Example = { "examplesentence", "ornekcumle", "örnekcümle" };
    public static readonly string[] ExampleMeaning = { "examplesentencemeaning", "examplesentencetr", "ornekcumletr" };
    public static readonly string[] Example2 = { "examplesentence2", "ornekcumle2", "örnekcümle2" };
    public static readonly string[] ExampleMeaning2 = { "examplesentencemeaning2", "examplesentencetr2", "ornekcumletr2" };

    /// <summary>Basligi karsilastirma icin sadelestirir: bosluk/alt tire atilir, kucuk harfe cevrilir.</summary>
    public static string Normalize(string header) =>
        header.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
}
