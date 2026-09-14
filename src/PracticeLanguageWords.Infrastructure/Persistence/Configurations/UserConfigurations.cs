using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PracticeLanguageWords.Domain.Entities;

namespace PracticeLanguageWords.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(200);

        // HasDefaultValue("") onemli: bu alanlar sonradan eklendigi icin veritabaninda
        // zaten kayitli kullanicilar (ornegin seed admin) olabilir. Varsayilan deger
        // sayesinde migration, mevcut satirlari NOT NULL ihlaline dusurmeden calisir.
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(200)
            .HasDefaultValue(string.Empty);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>(); // "Admin" / "User" olarak okunabilir sekilde saklanir

        builder.Property(u => u.CreatedAt).IsRequired();

        builder.HasIndex(u => u.Username).IsUnique();

        builder.HasOne(u => u.UserStreak)
            .WithOne(s => s.User)
            .HasForeignKey<UserStreak>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserStreakConfiguration : IEntityTypeConfiguration<UserStreak>
{
    public void Configure(EntityTypeBuilder<UserStreak> builder)
    {
        builder.ToTable("UserStreaks");
        builder.HasKey(s => s.UserId);

        builder.Property(s => s.CurrentStreak).IsRequired();
        builder.Property(s => s.LongestStreak).IsRequired();
        builder.Property(s => s.LastActiveDate).HasColumnType("date");
    }
}

public class UserStreakLogConfiguration : IEntityTypeConfiguration<UserStreakLog>
{
    public void Configure(EntityTypeBuilder<UserStreakLog> builder)
    {
        builder.ToTable("UserStreakLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.ActivityDate).HasColumnType("date").IsRequired();
        builder.Property(l => l.CardCount).IsRequired();

        // Streak artisinin idempotent olmasini garanti eden kisit:
        // bir kullanici icin bir gunde yalnizca tek satir olabilir.
        builder.HasIndex(l => new { l.UserId, l.ActivityDate }).IsUnique();

        builder.HasOne(l => l.User)
            .WithMany(u => u.StreakLogs)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
