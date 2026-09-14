using PracticeLanguageWords.Domain.Entities;

namespace PracticeLanguageWords.Application.Interfaces;

/// <summary>
/// Agirlikli rastgele kelime secim algoritmasi.
/// Zorluk derecesine gore agirliklandirir ve son gosterilen kelimeleri (anti-repeat)
/// haric tutmaya calisir.
/// </summary>
public interface IWordSelectionService
{
    /// <summary>Bir kategori icindeki kelimeler arasindan agirlikli secim yapar.</summary>
    Task<Word?> SelectFromCategoryAsync(int userId, int categoryId, IReadOnlyCollection<int> recentlyShownWordIds, CancellationToken ct = default);

    /// <summary>Sadece "Zor" isaretlenmis (bilinmeyen) kelimeler arasindan agirlikli secim yapar.</summary>
    Task<Word?> SelectFromUnknownAsync(int userId, int? languageId, IReadOnlyCollection<int> recentlyShownWordIds, CancellationToken ct = default);
}
