namespace PracticeLanguageWords.Application.DTOs;

/// <summary>
/// Bir lider tablosu satiri. DisplayName ad+soyad doludur; her ikisi de bossa
/// (migration oncesi kullanicilar) Username'e geri duser - bkz. LeaderboardService.
/// </summary>
/// <summary>LastSeen: "1sa", "7g", "3ay" gibi kisa Türkçe göreli zaman metni (bkz. RelativeTimeFormatter).</summary>
public record LeaderboardEntryDto(int Rank, string DisplayName, int WeeklyCardCount, string LastSeen, bool IsCurrentUser);

/// <summary>
/// Ana sayfada gosterilecek ozet: ilk 5 + (varsa ve ilk 5'te degilse) "Sen X. sirdasin" satiri.
/// </summary>
public record LeaderboardDto(
    IReadOnlyList<LeaderboardEntryDto> TopEntries,
    LeaderboardEntryDto? CurrentUserEntry,
    int TotalParticipants,
    DateOnly WeekStart,
    DateOnly WeekEnd);
