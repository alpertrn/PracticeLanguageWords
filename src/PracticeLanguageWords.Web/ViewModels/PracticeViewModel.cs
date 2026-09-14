using PracticeLanguageWords.Application.DTOs;

namespace PracticeLanguageWords.Web.ViewModels;

public class PracticeViewModel
{
    public PracticeSource Source { get; init; }
    public int? CategoryId { get; init; }
    public int LanguageId { get; init; }
    public string LanguageCode { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;

    /// <summary>Quiz modunda kullanici anlami yazar; kategori modunda zorluk butonlari gosterilir.</summary>
    public bool IsTypingMode => Source == PracticeSource.UnknownOnly;
}
