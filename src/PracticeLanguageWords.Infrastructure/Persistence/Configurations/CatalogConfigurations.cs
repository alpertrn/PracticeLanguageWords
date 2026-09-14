using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PracticeLanguageWords.Domain.Entities;

namespace PracticeLanguageWords.Infrastructure.Persistence.Configurations;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("Languages");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Code).IsRequired().HasMaxLength(10);
        builder.Property(l => l.Name).IsRequired().HasMaxLength(60);
        builder.Property(l => l.SpeechCode).IsRequired().HasMaxLength(20);
        builder.Property(l => l.IsActive).IsRequired();
        builder.Property(l => l.DisplayOrder).IsRequired();

        builder.HasIndex(l => l.Code).IsUnique();
    }
}

public class CategoryGroupConfiguration : IEntityTypeConfiguration<CategoryGroup>
{
    public void Configure(EntityTypeBuilder<CategoryGroup> builder)
    {
        builder.ToTable("CategoryGroups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        builder.Property(g => g.UiTemplate).IsRequired().HasMaxLength(50);

        builder.HasIndex(g => g.Name).IsUnique();
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);

        // Ayni dilde ayni kategori adi iki kez olamaz ("Almanca A1" tek olsun).
        builder.HasIndex(c => new { c.LanguageId, c.Name }).IsUnique();

        builder.HasOne(c => c.CategoryGroup)
            .WithMany(g => g.Categories)
            .HasForeignKey(c => c.CategoryGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Language)
            .WithMany(l => l.Categories)
            .HasForeignKey(c => c.LanguageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public void Configure(EntityTypeBuilder<Word> builder)
    {
        builder.ToTable("Words");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.TermText).IsRequired().HasMaxLength(200);
        builder.Property(w => w.MeaningText).IsRequired().HasMaxLength(300);
        builder.Property(w => w.TermRead).HasMaxLength(200);
        builder.Property(w => w.MemoryConnection).HasMaxLength(500);
        builder.Property(w => w.ExampleSentence).HasMaxLength(500);
        builder.Property(w => w.ExampleSentenceMeaning).HasMaxLength(500);
        builder.Property(w => w.ExampleSentence2).HasMaxLength(500);
        builder.Property(w => w.ExampleSentenceMeaning2).HasMaxLength(500);
        builder.Property(w => w.CreatedAt).IsRequired();

        // Ayni kategoride ayni kelime iki kez eklenemez (CSV import duplicate korumasi).
        builder.HasIndex(w => new { w.CategoryId, w.TermText }).IsUnique();

        // Kategori silinirken icinde kelime varsa engellenir (is kurali CategoryAdminService'te de var).
        builder.HasOne(w => w.Category)
            .WithMany(c => c.Words)
            .HasForeignKey(w => w.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
