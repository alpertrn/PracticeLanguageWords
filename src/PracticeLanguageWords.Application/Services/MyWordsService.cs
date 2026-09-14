using PracticeLanguageWords.Application.Common;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Domain.Enums;
using PracticeLanguageWords.Domain.Interfaces;

namespace PracticeLanguageWords.Application.Services;

public class MyWordsService : IMyWordsService
{
    private readonly IUnitOfWork _uow;
    private readonly ILanguageService _languageService;

    public MyWordsService(IUnitOfWork uow, ILanguageService languageService)
    {
        _uow = uow;
        _languageService = languageService;
    }

    public async Task<MyWordsDto> GetUnknownWordsAsync(int userId, int? languageId, CancellationToken ct = default)
    {
        var languages = await _languageService.GetActiveLanguagesAsync(ct);
        var selectedLanguageId = await _languageService.ResolveLanguageIdAsync(languageId, ct);
        var selected = languages.FirstOrDefault(l => l.Id == selectedLanguageId);

        var progresses = await _uow.WordProgresses.GetUnknownAsync(userId, selectedLanguageId, ct);

        var words = progresses
            .OrderBy(p => p.Word.TermText)
            .Select(p => new UnknownWordDto(
                p.WordId,
                p.Word.TermText,
                p.Word.TermRead,
                p.Word.MeaningText,
                p.Word.Category.Language.Name,
                p.Word.Category.Language.SpeechCode,
                p.Word.Category.Name))
            .ToList();

        return new MyWordsDto(
            languages,
            selectedLanguageId,
            selected?.Code ?? string.Empty,
            selected?.Name ?? string.Empty,
            words);
    }

    public async Task RemoveFromUnknownAsync(int userId, int wordId, CancellationToken ct = default)
    {
        var progress = await _uow.WordProgresses.GetAsync(userId, wordId, ct)
            ?? throw new NotFoundException("Kelime kaydı bulunamadı.");

        // Kayit silinmez; sadece "bilinmeyen" isareti kaldirilir ve kolay olarak isaretlenir.
        // Boylece kelime gecmisi (ReviewCount, LastReviewedAt) korunur.
        progress.IsUnknown = false;
        progress.Difficulty = DifficultyLevel.Easy;

        await _uow.SaveChangesAsync(ct);
    }
}
