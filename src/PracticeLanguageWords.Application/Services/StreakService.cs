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

    /// <summary>
    /// Bu haftanin (Pazartesi-Pazar, Türkiye saati) 7 günü - sirasi HER ZAMAN sabittir
    /// (Pzt, Sal, Çar, Per, Cum, Cmt, Paz). Eskiden "bugünden geriye son 7 gün" seklinde
    /// kayan bir pencereydi; bu durumda gün her gece bir sonraki hücreye kaydigi icin
    /// haftanin basi (Pazartesi) her gün farkli bir sutuna dusuyordu. Artik gunler yerinde
    /// sabit kaliyor, sadece o günün durumu (yapildi / henuz gelmedi / kacirildi) degisiyor.
    /// </summary>
    public async Task<IReadOnlyList<DayStatusDto>> GetLast7DaysAsync(int userId, CancellationToken ct = default)
    {
        var today = TurkeyClock.Today();

        // Pazartesi = 0 ... Pazar = 6 (DayOfWeek'te Pazar=0 oldugu icin +6 % 7 ile kaydiriyoruz).
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = today.AddDays(-daysSinceMonday);

        // Sadece Pazartesi'den bugune kadar olan araligin loglarina ihtiyac var;
        // gelecek gunlerin hicbir sekilde aktivitesi olamaz.
        var logs = await _uow.StreakLogs.GetLastNDaysAsync(userId, daysSinceMonday + 1, today, ct);
        var activeDates = logs.Select(l => l.ActivityDate).ToHashSet();

        var days = new List<DayStatusDto>();
        for (var i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            var isFuture = date > today;
            var active = !isFuture && activeDates.Contains(date);
            days.Add(new DayStatusDto(date, active, isFuture));
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
