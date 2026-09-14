using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Domain.Entities;
using PracticeLanguageWords.Domain.Enums;

namespace PracticeLanguageWords.Infrastructure.Persistence;

/// <summary>
/// Uygulama ilk kez ayaga kalktiginda bekleyen migration'lari uygular ve
/// baslangic verisini (admin kullanicisi, diller, ornek kategoriler ve kelimeler) olusturur.
/// Var olan veriyi asla ezmez - her adim once "zaten var mi" diye kontrol eder.
/// </summary>
public class DbSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<DbSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _context.Database.MigrateAsync(ct);

        await SeedAdminAsync(ct);
        await SeedLanguagesAsync(ct);
        await SeedCatalogAsync(ct);
    }

    private async Task SeedAdminAsync(CancellationToken ct)
    {
        var username = _configuration["SeedAdmin:Username"] ?? "admin";
        var password = _configuration["SeedAdmin:Password"] ?? "Admin123!";

        if (await _context.Users.AnyAsync(u => u.Username == username, ct))
        {
            return;
        }

        var admin = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.Hash(password),
            FirstName = _configuration["SeedAdmin:FirstName"] ?? "Yönetici",
            LastName = _configuration["SeedAdmin:LastName"] ?? string.Empty,
            Email = _configuration["SeedAdmin:Email"] ?? string.Empty,
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(admin);
        await _context.SaveChangesAsync(ct);

        _context.UserStreaks.Add(new UserStreak { UserId = admin.Id });
        await _context.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Baslangic admin kullanicisi olusturuldu: {Username}. Ilk giristen sonra sifreyi degistirin.",
            username);
    }

    private async Task SeedLanguagesAsync(CancellationToken ct)
    {
        if (await _context.Languages.AnyAsync(ct))
        {
            return;
        }

        _context.Languages.AddRange(
            new Language { Code = "en", Name = "İngilizce", SpeechCode = "en-US", IsActive = true, DisplayOrder = 1 },
            new Language { Code = "de", Name = "Almanca", SpeechCode = "de-DE", IsActive = true, DisplayOrder = 2 },
            new Language { Code = "fr", Name = "Fransızca", SpeechCode = "fr-FR", IsActive = true, DisplayOrder = 3 });

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Baslangic dilleri olusturuldu (İngilizce, Almanca, Fransızca).");
    }

    private async Task SeedCatalogAsync(CancellationToken ct)
    {
        if (await _context.CategoryGroups.AnyAsync(ct))
        {
            return;
        }

        var group = new CategoryGroup { Name = "PracticeWords", UiTemplate = "Flashcard" };
        _context.CategoryGroups.Add(group);
        await _context.SaveChangesAsync(ct);

        var english = await _context.Languages.FirstAsync(l => l.Code == "en", ct);
        var german = await _context.Languages.FirstAsync(l => l.Code == "de", ct);

        var enA1 = new Category { Name = "A1 Kelimeler", CategoryGroupId = group.Id, LanguageId = english.Id };
        var enPhrasal = new Category { Name = "Phrasal Verbs", CategoryGroupId = group.Id, LanguageId = english.Id };
        var deA1 = new Category { Name = "A1 Kelimeler", CategoryGroupId = group.Id, LanguageId = german.Id };

        _context.Categories.AddRange(enA1, enPhrasal, deA1);
        await _context.SaveChangesAsync(ct);

        _context.Words.AddRange(
            new Word
            {
                CategoryId = enA1.Id,
                TermText = "Reluctant",
                MeaningText = "isteksiz, gönülsüz",
                TermRead = "Rilaktınt",
                MemoryConnection = "RİLAx olan ve TAKıntı yapan kişi spor yapmaya İSTEKSİZDİR.",
                ExampleSentence = "She was reluctant to leave.",
                ExampleSentenceMeaning = "Ayrılmak konusunda isteksizdi.",
                ExampleSentence2 = "He seemed reluctant to answer the question.",
                ExampleSentenceMeaning2 = "Soruyu cevaplamak konusunda isteksiz görünüyordu."
            },
            new Word
            {
                CategoryId = enA1.Id,
                TermText = "Improve",
                MeaningText = "geliştirmek, iyileştirmek",
                TermRead = "İmpruv",
                MemoryConnection = "İMPARATOR PROVA yaparak kendini GELİŞTİRİR.",
                ExampleSentence = "I want to improve my English.",
                ExampleSentenceMeaning = "İngilizcemi geliştirmek istiyorum."
            },
            new Word
            {
                CategoryId = enPhrasal.Id,
                TermText = "Give up",
                MeaningText = "vazgeçmek, pes etmek",
                TermRead = "Giv ap",
                MemoryConnection = "Elindekini yukarı (UP) verirsen (GIVE) PES ETMİŞ olursun.",
                ExampleSentence = "Don't give up on your dreams.",
                ExampleSentenceMeaning = "Hayallerinden vazgeçme."
            },
            new Word
            {
                CategoryId = deA1.Id,
                TermText = "Anstrengend",
                MeaningText = "yorucu, zahmetli",
                TermRead = "Anştrengınt",
                MemoryConnection = "AN'ında STRES verip GENDİni yoran iş YORUCUDUR.",
                ExampleSentence = "Der Tag war sehr anstrengend.",
                ExampleSentenceMeaning = "Gün çok yorucuydu."
            },
            new Word
            {
                CategoryId = deA1.Id,
                TermText = "Wichtig",
                MeaningText = "önemli",
                TermRead = "Vihtiş",
                MemoryConnection = "VİCdanın TİTrediği konular ÖNEMLİdir.",
                ExampleSentence = "Das ist sehr wichtig für mich.",
                ExampleSentenceMeaning = "Bu benim için çok önemli."
            });

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Ornek kategori ve kelime verisi olusturuldu.");
    }
}
