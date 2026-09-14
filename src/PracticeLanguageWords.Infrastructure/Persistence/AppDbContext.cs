using Microsoft.EntityFrameworkCore;
using PracticeLanguageWords.Domain.Entities;

namespace PracticeLanguageWords.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<CategoryGroup> CategoryGroups => Set<CategoryGroup>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Word> Words => Set<Word>();
    public DbSet<UserWordProgress> UserWordProgresses => Set<UserWordProgress>();
    public DbSet<UserStreak> UserStreaks => Set<UserStreak>();
    public DbSet<UserStreakLog> UserStreakLogs => Set<UserStreakLog>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tum IEntityTypeConfiguration siniflari otomatik uygulanir.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
