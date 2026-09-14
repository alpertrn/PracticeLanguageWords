using Microsoft.EntityFrameworkCore;
using PracticeLanguageWords.Domain.Entities;
using PracticeLanguageWords.Domain.Interfaces;
using PracticeLanguageWords.Domain.Models;

namespace PracticeLanguageWords.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        _context.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default) =>
        _context.Users.AnyAsync(u => u.Username == username, ct);

    public void Add(User user) => _context.Users.Add(user);

    public async Task TouchLastSeenAsync(int userId, DateTime utcNow, CancellationToken ct = default)
    {
        // Read-modify-write yerine tek SQL UPDATE (bkz. UserStreakLogRepository.IncrementCardCountAsync).
        await _context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.LastSeenAt, utcNow), ct);
    }

    public Task<List<User>> GetNotificationCandidatesAsync(CancellationToken ct = default) =>
        _context.Users
            .Where(u => u.PushSubscriptions.Any())
            .Include(u => u.UserStreak)
            .Include(u => u.PushSubscriptions)
            .ToListAsync(ct);
}

public class PushSubscriptionRepository : IPushSubscriptionRepository
{
    private readonly AppDbContext _context;

    public PushSubscriptionRepository(AppDbContext context) => _context = context;

    public Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken ct = default) =>
        _context.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint, ct);

    public void Add(PushSubscription subscription) => _context.PushSubscriptions.Add(subscription);

    public void Remove(PushSubscription subscription) => _context.PushSubscriptions.Remove(subscription);
}

public class UserStreakRepository : IUserStreakRepository
{
    private readonly AppDbContext _context;

    public UserStreakRepository(AppDbContext context) => _context = context;

    public Task<UserStreak?> GetByUserIdAsync(int userId, CancellationToken ct = default) =>
        _context.UserStreaks.FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public void Add(UserStreak streak) => _context.UserStreaks.Add(streak);
}

public class UserStreakLogRepository : IUserStreakLogRepository
{
    private readonly AppDbContext _context;

    public UserStreakLogRepository(AppDbContext context) => _context = context;

    public Task<UserStreakLog?> GetForDateAsync(int userId, DateOnly date, CancellationToken ct = default) =>
        _context.UserStreakLogs.FirstOrDefaultAsync(l => l.UserId == userId && l.ActivityDate == date, ct);

    public Task<List<UserStreakLog>> GetLastNDaysAsync(int userId, int days, DateOnly today, CancellationToken ct = default)
    {
        var from = today.AddDays(-(days - 1));
        return _context.UserStreakLogs
            .AsNoTracking()
            .Where(l => l.UserId == userId && l.ActivityDate >= from && l.ActivityDate <= today)
            .ToListAsync(ct);
    }

    public async Task<bool> TryCreateForTodayAsync(int userId, DateOnly date, CancellationToken ct = default)
    {
        // Once ucuz kontrol: kayit zaten varsa exception maliyetine girme.
        var exists = await _context.UserStreakLogs
            .AnyAsync(l => l.UserId == userId && l.ActivityDate == date, ct);

        if (exists)
        {
            return false;
        }

        var log = new UserStreakLog { UserId = userId, ActivityDate = date, CardCount = 1 };
        _context.UserStreakLogs.Add(log);

        try
        {
            await _context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            // Yaris durumu: es zamanli bir istek ayni gunun kaydini bizden once olusturdu.
            // (UserId, ActivityDate) unique index bunu veritabani seviyesinde engelledi.
            _context.Entry(log).State = EntityState.Detached;
            return false;
        }
    }

    public async Task IncrementCardCountAsync(int userId, DateOnly date, CancellationToken ct = default)
    {
        // Read-modify-write yerine tek SQL UPDATE: es zamanli isteklerde sayac kaybolmaz.
        await _context.UserStreakLogs
            .Where(l => l.UserId == userId && l.ActivityDate == date)
            .ExecuteUpdateAsync(setters => setters.SetProperty(l => l.CardCount, l => l.CardCount + 1), ct);
    }

    public Task<List<LeaderboardRow>> GetWeeklyLeaderboardAsync(DateOnly weekStart, DateOnly weekEnd, CancellationToken ct = default) =>
        _context.UserStreakLogs
            .Where(l => l.ActivityDate >= weekStart && l.ActivityDate <= weekEnd)
            .GroupBy(l => l.UserId)
            .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.CardCount) })
            .Join(_context.Users, x => x.UserId, u => u.Id, (x, u) =>
                new { u.Id, u.Username, u.FirstName, u.LastName, x.Total, u.LastSeenAt })
            // Esit puanda siralamanin her seferinde ayni (kararli) cikmasi icin kullanici adina gore ikincil siralama.
            // Siralama, LeaderboardRow (record) uzerinden degil dogrudan anonim tip alanlari
            // uzerinden yapilir - EF Core, kayit (record) constructor'i uzerinden property
            // erisimini SQL'e cevirimeyebiliyor (bkz. "could not be translated" hatasi).
            .OrderByDescending(r => r.Total)
            .ThenBy(r => r.Username)
            .Select(r => new LeaderboardRow(r.Id, r.Username, r.FirstName, r.LastName, r.Total, r.LastSeenAt))
            .ToListAsync(ct);
}
