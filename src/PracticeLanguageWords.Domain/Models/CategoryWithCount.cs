namespace PracticeLanguageWords.Domain.Models;

/// <summary>Kategori + kelime sayisi projeksiyonu (tum kelimeleri belleğe cekmemek icin).</summary>
public record CategoryWithCount(
    int Id,
    string Name,
    int CategoryGroupId,
    string CategoryGroupName,
    int LanguageId,
    string LanguageName,
    int WordCount);
