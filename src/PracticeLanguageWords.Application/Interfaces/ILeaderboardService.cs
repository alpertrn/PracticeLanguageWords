using PracticeLanguageWords.Application.DTOs;

namespace PracticeLanguageWords.Application.Interfaces;

public interface ILeaderboardService
{
    /// <summary>Ana sayfa ozeti: bu haftanin ilk 5'i + (varsa) mevcut kullanicinin sirasi.</summary>
    Task<LeaderboardDto> GetWeeklyTopAsync(int currentUserId, CancellationToken ct = default);

    /// <summary>"Tüm kullanıcıları göster" ile acilan, bu hafta aktivitesi olan herkesi iceren tam liste.</summary>
    Task<IReadOnlyList<LeaderboardEntryDto>> GetWeeklyFullListAsync(int currentUserId, CancellationToken ct = default);
}
