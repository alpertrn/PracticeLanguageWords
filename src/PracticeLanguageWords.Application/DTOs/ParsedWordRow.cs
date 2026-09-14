namespace PracticeLanguageWords.Application.DTOs;

/// <summary>
/// CSV/Excel'den okunan ham satir. Import parserlari (IWordImportParser) bunu uretir,
/// WordAdminService bunu Word entity'sine cevirir ve duplicate kontrolu yapar.
/// </summary>
public record ParsedWordRow(
    int RowNumber,
    string TermText,
    string MeaningText,
    string? TermRead,
    string? MemoryConnection,
    string? ExampleSentence,
    string? ExampleSentenceMeaning,
    string? ExampleSentence2 = null,
    string? ExampleSentenceMeaning2 = null);
