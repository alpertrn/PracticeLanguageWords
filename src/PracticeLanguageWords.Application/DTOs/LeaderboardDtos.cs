namespace PracticeLanguageWords.Application.DTOs;

/// <summary>
/// Bir lider tablosu satiri. DisplayName ad+soyad doludur; her ikisi de bossa
/// (migration oncesi kullanicilar) Username'e geri duser - bkz. LeaderboardService.
/// </summary>
public record LeaderboardEntryDto(int Rank, string DisplayName, int WeeklyCardCount, bool IsCurrentUser);

/// <summary>
/// Ana sayfada gosterilecek ozet: ilk 5 + (varsa ve ilk 5'te degilse) "Sen X. sirdasin" satiri.
/// </summary>
public record LeaderboardDto(
    IReadOnlyList<LeaderboardEntryDto> TopEntries,
    LeaderboardEntryDto? CurrentUserEntry,
    int TotalParticipants,
    DateOnly WeekStart,
    DateOnly WeekEnd);
