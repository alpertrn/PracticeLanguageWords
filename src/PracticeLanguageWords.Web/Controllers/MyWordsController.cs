using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Web.Controllers;

[Authorize]
public class MyWordsController : LanguageAwareController
{
    private readonly IMyWordsService _myWordsService;
    private readonly ICurrentUserService _currentUser;

    public MyWordsController(
        IMyWordsService myWordsService,
        ICurrentUserService currentUser,
        ILanguageService languageService) : base(languageService)
    {
        _myWordsService = myWordsService;
        _currentUser = currentUser;
    }

    /// <summary>Bilmediğim kelimeler: /en/my-words, /de/my-words ...</summary>
    [HttpGet("{langCode:alpha:length(2)}/my-words")]
    public async Task<IActionResult> Index(string langCode, CancellationToken ct)
    {
        var (language, redirect) = await ResolveLanguageAsync(langCode, "/my-words", ct);
        if (redirect is not null)
        {
            return redirect;
        }

        var model = await _myWordsService.GetUnknownWordsAsync(_currentUser.UserId, language!.Id, ct);
        return View(model);
    }

    /// <summary>AJAX: kelimeyi bilinmeyenler listesinden cikarir (sayfa yenilenmez).</summary>
    [HttpPost("api/my-words/{wordId:int}/remove")]
    public async Task<IActionResult> Remove(int wordId, CancellationToken ct)
    {
        await _myWordsService.RemoveFromUnknownAsync(_currentUser.UserId, wordId, ct);
        return Json(new { success = true });
    }
}
