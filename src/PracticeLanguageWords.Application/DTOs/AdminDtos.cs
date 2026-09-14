namespace PracticeLanguageWords.Application.DTOs;

public record LanguageDto(int Id, string Code, string Name, string SpeechCode, bool IsActive, int DisplayOrder, int CategoryCount);

public record LanguageEditDto(int Id, string Code, string Name, string SpeechCode, bool IsActive, int DisplayOrder);

public record CategoryGroupDto(int Id, string Name, string UiTemplate, int CategoryCount);

public record CategoryGroupEditDto(int Id, string Name, string UiTemplate);

public record CategoryAdminDto(
    int Id,
    string Name,
    int CategoryGroupId,
    string CategoryGroupName,
    int LanguageId,
    string LanguageName,
    int WordCount);

public record CategoryEditDto(int Id, string Name, int CategoryGroupId, int LanguageId);

public record WordAdminDto(
    int Id,
    int CategoryId,
    string CategoryName,
    string LanguageName,
    string TermText,
    string MeaningText,
    string? TermRead,
    string? MemoryConnection,
    string? ExampleSentence,
    string? ExampleSentenceMeaning,
    string? ExampleSentence2,
    string? ExampleSentenceMeaning2);

public record WordEditDto(
    int? Id,
    int CategoryId,
    string TermText,
    string MeaningText,
    string? TermRead,
    string? MemoryConnection,
    string? ExampleSentence,
    string? ExampleSentenceMeaning,
    string? ExampleSentence2,
    string? ExampleSentenceMeaning2);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public record ImportRowError(int RowNumber, string TermText, string Reason);

public record ImportResultDto(int Inserted, int Skipped, IReadOnlyList<ImportRowError> Errors);
