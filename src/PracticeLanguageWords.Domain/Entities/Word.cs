namespace PracticeLanguageWords.Domain.Entities;

/// <summary>
/// Ogrenilen kelime. Alan adlari dilden bagimsizdir: TermText hedef dildeki kelime
/// (Ingilizce/Almanca/Fransizca), MeaningText ise Turkce karsiligidir.
/// </summary>
public class Word
{
    public int Id { get; set; }
    public int CategoryId { get; set; }

    /// <summary>Hedef dildeki kelime (orn. "Reluctant", "Anstrengend").</summary>
    public string TermText { get; set; } = string.Empty;

    /// <summary>
    /// Turkce anlam(lar). Birden fazla anlam virgul ile ayrilir (orn: "gulmek, gulumsemek").
    /// Quiz modunda kullanici bunlardan sadece birini yazarsa dogru kabul edilir.
    /// </summary>
    public string MeaningText { get; set; } = string.Empty;

    /// <summary>Turkce okunusu (orn. "Rilaktınt").</summary>
    public string? TermRead { get; set; }

    public string? MemoryConnection { get; set; }

    /// <summary>Hedef dilde ornek cumle (1.).</summary>
    public string? ExampleSentence { get; set; }

    /// <summary>Ornek cumlenin (1.) Turkce cevirisi.</summary>
    public string? ExampleSentenceMeaning { get; set; }

    /// <summary>Hedef dilde ikinci (opsiyonel) ornek cumle.</summary>
    public string? ExampleSentence2 { get; set; }

    /// <summary>Ikinci ornek cumlenin Turkce cevirisi.</summary>
    public string? ExampleSentenceMeaning2 { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Category Category { get; set; } = null!;
    public ICollection<UserWordProgress> UserProgresses { get; set; } = new List<UserWordProgress>();

    /// <summary>
    /// MeaningText alanindaki virgulle ayrilmis kabul edilebilir anlamlarin listesi.
    /// Tek bir yerde parse edilir; cevap kontrolu bunu kullanir.
    /// </summary>
    public IReadOnlyList<string> GetAcceptedMeanings()
    {
        if (string.IsNullOrWhiteSpace(MeaningText))
        {
            return Array.Empty<string>();
        }

        return MeaningText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(m => m.Length > 0)
            .ToList();
    }
}
