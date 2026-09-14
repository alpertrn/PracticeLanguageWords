using PracticeLanguageWords.Application.Common;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Domain.Entities;
using PracticeLanguageWords.Domain.Interfaces;

namespace PracticeLanguageWords.Application.Services;

public class StreakService : IStreakService
{
    private const int MilestoneEvery = 5;
    private readonly IUnitOfWork _uow;

    public StreakService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<StreakInfoDto> GetStreakAsync(int userId, CancellationToken ct = default)
    {
        var streak = await _uow.Streaks.GetByUserIdAsync(userId, ct);
        return new StreakInfoDto(streak?.CurrentStreak ?? 0, streak?.LongestStreak ?? 0);
    }

    public async Task<IReadOnlyList<DayStatusDto>> GetLast7DaysAsync(int userId, CancellationToken ct = default)
    {
        var today = TurkeyClock.Today();
        var logs = await _uow.StreakLogs.GetLastNDaysAsync(userId, 7, today, ct);
        var activeDates = logs.Select(l => l.ActivityDate).ToHashSet();

        var days = new List<DayStatusDto>();
        for (var i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            days.Add(new DayStatusDto(date, activeDates.Contains(date)));
        }

        return days;
    }

    public async Task<(StreakInfoDto Streak, bool MilestoneReached)> RegisterActivityAsync(int userId, CancellationToken ct = default)
    {
        var today = TurkeyClock.Today();

        // Atomik "ilk kayit" denemesi: iki es zamanli istek gelirse sadece biri true doner.
        var isFirstActivityToday = await _uow.StreakLogs.TryCreateForTodayAsync(userId, today, ct);

        if (!isFirstActivityToday)
        {
            // Bugun zaten en az bir kart isaretlenmis; sadece gunluk kart sayacini artir,
            // seri sayisini tekrar artirma (idempotentlik).
            await _uow.StreakLogs.IncrementCardCountAsync(userId, today, ct);
            var current = await GetStreakAsync(userId, ct);
            return (current, false);
        }

        var milestoneReached = false;
        StreakInfoDto result = new(0, 0);

        await _uow.ExecuteInTransactionAsync(async () =>
        {
            var streak = await _uow.Streaks.GetByUserIdAsync(userId, ct);
            if (streak is null)
            {
                streak = new UserStreak { UserId = userId, CurrentStreak = 0, LongestStreak = 0 };
                _uow.Streaks.Add(streak);
            }

            var yesterday = today.AddDays(-1);
            var newCurrent = streak.LastActiveDate == yesterday
                ? streak.CurrentStreak + 1
                : 1; // ilk aktivite ya da bir gun atlanmis (streak kirilmasi)

            streak.CurrentStreak = newCurrent;
            streak.LongestStreak = Math.Max(streak.LongestStreak, newCurrent);
            streak.LastActiveDate = today;

            await _uow.SaveChangesAsync(ct);

            milestoneReached = newCurrent > 0 && newCurrent % MilestoneEvery == 0;
            result = new StreakInfoDto(streak.CurrentStreak, streak.LongestStreak);
        }, ct);

        return (result, milestoneReached);
    }
}
