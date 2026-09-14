using PracticeLanguageWords.Application.Common;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Domain.Interfaces;

namespace PracticeLanguageWords.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;
    private readonly IStreakService _streakService;
    private readonly ILanguageService _languageService;
    private readonly ILeaderboardService _leaderboardService;

    public DashboardService(
        IUnitOfWork uow,
        IStreakService streakService,
        ILanguageService languageService,
        ILeaderboardService leaderboardService)
    {
        _uow = uow;
        _streakService = streakService;
        _languageService = languageService;
        _leaderboardService = leaderboardService;
    }

    public async Task<DashboardDto> GetDashboardAsync(int userId, int? languageId, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("Kullanıcı bulunamadı.");

        var streak = await _streakService.GetStreakAsync(userId, ct);
        var last7Days = await _streakService.GetLast7DaysAsync(userId, ct);

        var languages = await _languageService.GetActiveLanguagesAsync(ct);
        var selectedLanguageId = await _languageService.ResolveLanguageIdAsync(languageId, ct);
        var selected = languages.FirstOrDefault(l => l.Id == selectedLanguageId);

        var categories = await _uow.Categories.GetAllWithWordCountAsync(selectedLanguageId, ct);
        var unknownCount = await _uow.WordProgresses.CountUnknownAsync(userId, selectedLanguageId, ct);
        var leaderboard = await _leaderboardService.GetWeeklyTopAsync(userId, ct);

        var categoryDtos = categories
            .Select(c => new CategorySummaryDto(c.Id, c.Name, c.WordCount))
            .OrderBy(c => c.Name)
            .ToList();

        // Karsilama mesajinda kullanici adi yerine gercek isim gosterilir (lider tablosundaki
        // "ad soyad" davranisiyla tutarli - bkz. LeaderboardService.BuildRanked). Ikisi de
        // bossa (eski kullanici, henuz guncellenmemis) kullanici adina geri dusulur.
        var displayName = string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName)
            ? user.Username
            : $"{user.FirstName} {user.LastName}".Trim();

        return new DashboardDto(
            displayName,
            streak,
            last7Days,
            languages,
            selectedLanguageId,
            selected?.Code ?? string.Empty,
            selected?.Name ?? string.Empty,
            categoryDtos,
            unknownCount,
            leaderboard);
    }
}

public class LanguageService : ILanguageService
{
    private readonly IUnitOfWork _uow;

    public LanguageService(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<LanguageSummaryDto>> GetActiveLanguagesAsync(CancellationToken ct = default)
    {
        var languages = await _uow.Languages.GetAllAsync(onlyActive: true, ct);
        var categories = await _uow.Categories.GetAllWithWordCountAsync(null, ct);

        return languages
            .Select(l => new LanguageSummaryDto(
                l.Id,
                l.Code,
                l.Name,
                categories.Count(c => c.LanguageId == l.Id)))
            .ToList();
    }

    public async Task<LanguageSummaryDto?> GetByCodeAsync(string? code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var languages = await GetActiveLanguagesAsync(ct);
        return languages.FirstOrDefault(l => string.Equals(l.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<LanguageSummaryDto?> GetDefaultAsync(CancellationToken ct = default)
    {
        var languages = await GetActiveLanguagesAsync(ct);
        return languages.FirstOrDefault();
    }

    public async Task<int> ResolveLanguageIdAsync(int? requestedLanguageId, CancellationToken ct = default)
    {
        var languages = await _uow.Languages.GetAllAsync(onlyActive: true, ct);

        if (languages.Count == 0)
        {
            return 0;
        }

        if (requestedLanguageId is not null && languages.Any(l => l.Id == requestedLanguageId.Value))
        {
            return requestedLanguageId.Value;
        }

        return languages[0].Id;
    }
}
