namespace PracticeLanguageWords.Domain.Interfaces;

/// <summary>
/// Bir islem (transaction) icindeki tum repository'leri ve SaveChanges'i tek noktadan yonetir.
/// </summary>
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    ILanguageRepository Languages { get; }
    ICategoryGroupRepository CategoryGroups { get; }
    ICategoryRepository Categories { get; }
    IWordRepository Words { get; }
    IUserWordProgressRepository WordProgresses { get; }
    IUserStreakRepository Streaks { get; }
    IUserStreakLogRepository StreakLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Streak guncellemesi gibi birden fazla tabloyu ilgilendiren, atomik olmasi gereken
    /// islemler icin veritabani transaction'i acar.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
}
