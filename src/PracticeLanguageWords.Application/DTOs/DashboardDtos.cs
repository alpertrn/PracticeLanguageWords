namespace PracticeLanguageWords.Application.DTOs;

public record StreakInfoDto(int CurrentStreak, int LongestStreak);

/// <summary>IsFuture: bu hafta icinde ama henuz gelmemis gun (bugunden sonrasi) - "kacirilmis" sayilmaz.</summary>
public record DayStatusDto(DateOnly Date, bool Active, bool IsFuture);

public record CategorySummaryDto(int Id, string Name, int WordCount);

public record LanguageSummaryDto(int Id, string Code, string Name, int CategoryCount);

public record DashboardDto(
    string Username,
    StreakInfoDto Streak,
    IReadOnlyList<DayStatusDto> Last7Days,
    IReadOnlyList<LanguageSummaryDto> Languages,
    int SelectedLanguageId,
    string SelectedLanguageCode,
    string SelectedLanguageName,
    IReadOnlyList<CategorySummaryDto> Categories,
    int UnknownWordCount,
    LeaderboardDto Leaderboard);

public record UnknownWordDto(
    int WordId,
    string TermText,
    string? TermRead,
    string MeaningText,
    string LanguageName,
    /// <summary>Web Speech API dil kodu — satirdaki "Dinle" butonu bu dille okur.</summary>
    string SpeechCode,
    string CategoryName);

public record MyWordsDto(
    IReadOnlyList<LanguageSummaryDto> Languages,
    int SelectedLanguageId,
    string SelectedLanguageCode,
    string SelectedLanguageName,
    IReadOnlyList<UnknownWordDto> Words);
