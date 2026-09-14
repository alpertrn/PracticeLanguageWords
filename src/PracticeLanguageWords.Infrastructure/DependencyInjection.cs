using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Application.Services;
using PracticeLanguageWords.Domain.Interfaces;
using PracticeLanguageWords.Infrastructure.Import;
using PracticeLanguageWords.Infrastructure.Persistence;
using PracticeLanguageWords.Infrastructure.Security;

namespace PracticeLanguageWords.Infrastructure;

/// <summary>
/// Tum altyapi ve uygulama servislerinin DI kayitlari tek noktada toplanir.
/// Web katmani sadece bu metodu cagirir; hangi somut sinifin kullanildigini bilmez.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPracticeLanguageWords(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("'ConnectionStrings:DefaultConnection' tanimli degil.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                sql.CommandTimeout(30);
            }));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Application servisleri
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IStreakService, StreakService>();
        services.AddScoped<IWordSelectionService, WordSelectionService>();
        services.AddScoped<IPracticeService, PracticeService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<IMyWordsService, MyWordsService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<ILanguageAdminService, LanguageAdminService>();
        services.AddScoped<ICategoryGroupAdminService, CategoryGroupAdminService>();
        services.AddScoped<ICategoryAdminService, CategoryAdminService>();
        services.AddScoped<IWordAdminService, WordAdminService>();

        // Altyapi servisleri
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IAnswerEvaluator, AnswerEvaluator>();

        // Seviye 1 telaffuz degerlendirmesi: tarayicidan gelen metni karsilastirir (ucretsiz, anahtarsiz).
        // Fonem bazli puanlamaya gecilirse burada baska bir implementasyon kaydedilir.
        services.AddSingleton<IPronunciationEvaluator, PronunciationEvaluator>();

        // Import parserlari (OCP: yeni format = yeni kayit)
        services.AddScoped<IWordImportParser, CsvWordImportParser>();
        services.AddScoped<IWordImportParser, ExcelWordImportParser>();
        services.AddScoped<IWordImportParserFactory, WordImportParserFactory>();

        services.AddScoped<DbSeeder>();

        return services;
    }
}
