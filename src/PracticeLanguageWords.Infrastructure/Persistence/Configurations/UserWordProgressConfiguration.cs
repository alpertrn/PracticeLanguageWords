using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PracticeLanguageWords.Domain.Entities;

namespace PracticeLanguageWords.Infrastructure.Persistence.Configurations;

public class UserWordProgressConfiguration : IEntityTypeConfiguration<UserWordProgress>
{
    public void Configure(EntityTypeBuilder<UserWordProgress> builder)
    {
        builder.ToTable("UserWordProgresses");

        builder.HasKey(p => new { p.UserId, p.WordId });

        builder.Property(p => p.Difficulty)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(p => p.IsUnknown).IsRequired();
        builder.Property(p => p.ReviewCount).IsRequired();
        builder.Property(p => p.LastReviewedAt).IsRequired();
        builder.Property(p => p.MasteryStreak).IsRequired();
        builder.Property(p => p.PronunciationAttempts).IsRequired();
        builder.Property(p => p.PronunciationSuccesses).IsRequired();

        // "Bilmedigim kelimeler" sorgusu ve agirlikli secim icin kapsayici index.
        builder.HasIndex(p => new { p.UserId, p.IsUnknown });

        builder.HasOne(p => p.User)
            .WithMany(u => u.WordProgresses)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // GEREKSINIM: Admin bir kelimeyi sildiginde, o kelime tum kullanicilarin
        // ilerleme/bilinmeyen listelerinden de otomatik olarak silinir.
        builder.HasOne(p => p.Word)
            .WithMany(w => w.UserProgresses)
            .HasForeignKey(p => p.WordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
