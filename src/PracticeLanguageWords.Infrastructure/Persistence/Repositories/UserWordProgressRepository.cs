using Microsoft.EntityFrameworkCore;
using PracticeLanguageWords.Domain.Entities;
using PracticeLanguageWords.Domain.Enums;
using PracticeLanguageWords.Domain.Interfaces;

namespace PracticeLanguageWords.Infrastructure.Persistence.Repositories;

public class UserWordProgressRepository : IUserWordProgressRepository
{
    private readonly AppDbContext _context;

    public UserWordProgressRepository(AppDbContext context) => _context = context;

    public Task<UserWordProgress?> GetAsync(int userId, int wordId, CancellationToken ct = default) =>
        _context.UserWordProgresses.FirstOrDefaultAsync(p => p.UserId == userId && p.WordId == wordId, ct);

    public async Task<UserWordProgress> GetOrCreateAsync(int userId, int wordId, CancellationToken ct = default)
    {
        var progress = await GetAsync(userId, wordId, ct);

        if (progress is not null)
        {
            return progress;
        }

        progress = new UserWordProgress
        {
            UserId = userId,
            WordId = wordId,
            Difficulty = DifficultyLevel.Medium,
            ReviewCount = 0
        };

        _context.UserWordProgresses.Add(progress);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Yaris durumu: es zamanli baska bir istek ayni (UserId, WordId) kaydini
            // bizden once olusturdu (composite PK ihlali - bkz. UserStreakLogRepository.
            // TryCreateForTodayAsync, ayni desen). Kendi eklememizi izlemeden cikar
            // (henuz DB'ye yazilmadigi icin DELETE calismaz, sadece detach eder) ve
            // kazanan kaydi oku.
            _context.Entry(progress).State = EntityState.Detached;

            progress = await GetAsync(userId, wordId, ct)
                ?? throw new InvalidOperationException(
                    "Kelime ilerleme kaydi olusturulamadi ve tekrar okunamadi.");
        }

        return progress;
    }

    public Task<List<UserWordProgress>> GetUnknownAsync(int userId, int? languageId, CancellationToken ct = default) =>
        BuildUnknownQuery(userId, languageId)
            .AsNoTracking()
            .Include(p => p.Word)
            .ThenInclude(w => w.Category)
            .ThenInclude(c => c.Language)
            .ToListAsync(ct);

    public Task<int> CountUnknownAsync(int userId, int? languageId, CancellationToken ct = default) =>
        BuildUnknownQuery(userId, languageId).CountAsync(ct);

    public Task<int> CountUnknownAvailableTodayAsync(int userId, int? languageId, DateOnly today, CancellationToken ct = default) =>
        BuildUnknownQuery(userId, languageId)
            .Where(p => p.LastMasteryAttemptDate == null || p.LastMasteryAttemptDate != today)
            .CountAsync(ct);

    public void Add(UserWordProgress progress) => _context.UserWordProgresses.Add(progress);

    public void Remove(UserWordProgress progress) => _context.UserWordProgresses.Remove(progress);

    private IQueryable<UserWordProgress> BuildUnknownQuery(int userId, int? languageId)
    {
        var query = _context.UserWordProgresses.Where(p => p.UserId == userId && p.IsUnknown);

        if (languageId is not null && languageId > 0)
        {
            query = query.Where(p => p.Word.Category.LanguageId == languageId);
        }

        return query;
    }
}
