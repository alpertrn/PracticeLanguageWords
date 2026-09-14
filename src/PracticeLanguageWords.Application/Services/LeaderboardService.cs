using PracticeLanguageWords.Application.Common;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Domain.Interfaces;
using PracticeLanguageWords.Domain.Models;

namespace PracticeLanguageWords.Application.Services;

/// <summary>
/// Haftalik lider tablosu: puan = o hafta (Pazartesi-Pazar, Türkiye saati) cözülen toplam
/// kart sayisi (UserStreakLogs.CardCount toplami - kategori pratiginde Kolay/Orta/Hiç
/// Bilmiyorum ile cevaplanan + quizde cevaplanan tüm kartlar dahildir). Hafta degisince
/// (Pazartesi 00:00 TR saati) tablo otomatik sifirlanir - ayri bir "sifirlama" islemi gerekmez,
/// cünkü sorgu her zaman o anki hafta araligina gore calisir.
/// </summary>
public class LeaderboardService : ILeaderboardService
{
    private const int TopCount = 5;
    private readonly IUnitOfWork _uow;

    public LeaderboardService(IUnitOfWork uow) => _uow = uow;

    public async Task<LeaderboardDto> GetWeeklyTopAsync(int currentUserId, CancellationToken ct = default)
    {
        var (weekStart, weekEnd) = GetCurrentWeekRange();
        var rows = await _uow.StreakLogs.GetWeeklyLeaderboardAsync(weekStart, weekEnd, ct);
        var ranked = BuildRanked(rows, currentUserId);

        var top = ranked.Take(TopCount).ToList();

        // Kullanici zaten ilk 5'teyse ayrica "Sen X. sirdasin" satirina gerek yok.
        var currentEntry = ranked.FirstOrDefault(e => e.IsCurrentUser);
        var currentUserBelowTop = currentEntry is not null && currentEntry.Rank > TopCount ? currentEntry : null;

        return new LeaderboardDto(top, currentUserBelowTop, ranked.Count, weekStart, weekEnd);
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetWeeklyFullListAsync(int currentUserId, CancellationToken ct = default)
    {
        var (weekStart, weekEnd) = GetCurrentWeekRange();
        var rows = await _uow.StreakLogs.GetWeeklyLeaderboardAsync(weekStart, weekEnd, ct);
        return BuildRanked(rows, currentUserId);
    }

    private static List<LeaderboardEntryDto> BuildRanked(List<LeaderboardRow> rows, int currentUserId)
    {
        var ranked = new List<LeaderboardEntryDto>(rows.Count);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var displayName = string.IsNullOrWhiteSpace(row.FirstName) && string.IsNullOrWhiteSpace(row.LastName)
                ? row.Username
                : $"{row.FirstName} {row.LastName}".Trim();

            ranked.Add(new LeaderboardEntryDto(i + 1, displayName, row.WeeklyCardCount, row.UserId == currentUserId));
        }

        return ranked;
    }

    private static (DateOnly WeekStart, DateOnly WeekEnd) GetCurrentWeekRange()
    {
        var today = TurkeyClock.Today();

        // Pazartesi haftanin ilk günü sayilir (TR standardi / ISO 8601).
        // DayOfWeek: Pazar=0, Pazartesi=1, ... Cumartesi=6 -> Pazartesi'ye kac gün var hesabi.
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = today.AddDays(-daysSinceMonday);
        var weekEnd = weekStart.AddDays(6);

        return (weekStart, weekEnd);
    }
}
