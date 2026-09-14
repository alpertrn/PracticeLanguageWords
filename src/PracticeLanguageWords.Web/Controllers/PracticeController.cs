using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Web.ViewModels;

namespace PracticeLanguageWords.Web.Controllers;

[Authorize]
public class PracticeController : LanguageAwareController
{
    private readonly IPracticeService _practiceService;
    private readonly ICurrentUserService _currentUser;

    public PracticeController(
        IPracticeService practiceService,
        ICurrentUserService currentUser,
        ILanguageService languageService) : base(languageService)
    {
        _practiceService = practiceService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Kategori pratigi (/en/practice/5): kart tum bilgisiyle gosterilir,
    /// kullanici Kolay / Orta / Hiç Bilmiyorum secer. Sonsuz dongu.
    /// </summary>
    [HttpGet("{langCode:alpha:length(2)}/practice/{categoryId:int}")]
    public Task<IActionResult> Practice(string langCode, int categoryId, CancellationToken ct) =>
        StartAsync(langCode, $"/practice/{categoryId}", PracticeSource.Category, categoryId, ct);

    /// <summary>Bilmediğim kelimeler quizi (/en/quiz): anlam gizli, kullanici yazarak cevaplar.</summary>
    [HttpGet("{langCode:alpha:length(2)}/quiz")]
    public Task<IActionResult> Quiz(string langCode, CancellationToken ct) =>
        StartAsync(langCode, "/quiz", PracticeSource.UnknownOnly, null, ct);

    private async Task<IActionResult> StartAsync(
        string langCode,
        string relativePath,
        PracticeSource source,
        int? categoryId,
        CancellationToken ct)
    {
        var (language, redirect) = await ResolveLanguageAsync(langCode, relativePath, ct);
        if (redirect is not null)
        {
            return redirect;
        }

        var context = await _practiceService.GetContextAsync(_currentUser.UserId, source, categoryId, language!.Id, ct);

        if (!context.CanStart)
        {
            TempData[source == PracticeSource.UnknownOnly ? "Success" : "Info"] = context.RedirectMessage;
            return Redirect($"/{language.Code}");
        }

        return View("Card", new PracticeViewModel
        {
            Source = source,
            CategoryId = categoryId,
            LanguageId = language.Id,
            LanguageCode = language.Code,
            Title = context.Title,
            Subtitle = context.Subtitle
        });
    }
}
