namespace PracticeLanguageWords.Domain.Entities;

/// <summary>
/// Ogrenilen dil (Ingilizce, Almanca, Fransizca ...).
/// Kategoriler bir dile baglidir; kullanici ana sayfada dil secerek calisir.
/// </summary>
public class Language
{
    public int Id { get; set; }

    /// <summary>ISO kodu: "en", "de", "fr".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Kullaniciya gosterilen ad: "İngilizce".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Web Speech API dil kodu: "en-US", "de-DE", "fr-FR".</summary>
    public string SpeechCode { get; set; } = string.Empty;

    /// <summary>Pasif diller ana sayfada listelenmez (veri silinmeden gizlenebilir).</summary>
    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
