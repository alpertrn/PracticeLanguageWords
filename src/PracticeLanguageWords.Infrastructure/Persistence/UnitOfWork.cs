using Microsoft.EntityFrameworkCore;
using PracticeLanguageWords.Domain.Interfaces;
using PracticeLanguageWords.Infrastructure.Persistence.Repositories;

namespace PracticeLanguageWords.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    private IUserRepository? _users;
    private ILanguageRepository? _languages;
    private ICategoryGroupRepository? _categoryGroups;
    private ICategoryRepository? _categories;
    private IWordRepository? _words;
    private IUserWordProgressRepository? _wordProgresses;
    private IUserStreakRepository? _streaks;
    private IUserStreakLogRepository? _streakLogs;
    private IPushSubscriptionRepository? _pushSubscriptions;

    public UnitOfWork(AppDbContext context) => _context = context;

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public ILanguageRepository Languages => _languages ??= new LanguageRepository(_context);
    public ICategoryGroupRepository CategoryGroups => _categoryGroups ??= new CategoryGroupRepository(_context);
    public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
    public IWordRepository Words => _words ??= new WordRepository(_context);
    public IUserWordProgressRepository WordProgresses => _wordProgresses ??= new UserWordProgressRepository(_context);
    public IUserStreakRepository Streaks => _streaks ??= new UserStreakRepository(_context);
    public IUserStreakLogRepository StreakLogs => _streakLogs ??= new UserStreakLogRepository(_context);
    public IPushSubscriptionRepository PushSubscriptions => _pushSubscriptions ??= new PushSubscriptionRepository(_context);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
    {
        // EnableRetryOnFailure ile birlikte transaction kullanmak icin execution strategy sart.
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                await action();
                await transaction.CommitAsync(ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }
}
