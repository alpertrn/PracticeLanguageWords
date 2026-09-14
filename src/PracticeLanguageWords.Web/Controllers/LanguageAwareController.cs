using Microsoft.AspNetCore.Mvc;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Web.Controllers;

/// <summary>
/// URL'nin ilk parcasindaki dil kodunu ("/en/my-words" -> "en") cozer.
/// Kod gecersizse kullaniciyi varsayilan dilin ayni sayfasina yonlendirir;
/// hic dil tanimli degilse bilgilendirme ile ana sayfaya doner.
/// </summary>
public abstract class LanguageAwareController : Controller
{
    protected readonly ILanguageService LanguageService;

    protected LanguageAwareController(ILanguageService languageService) => LanguageService = languageService;

    /// <param name="relativePath">Dil kodundan sonraki yol, orn. "/my-words" veya "" (ana sayfa).</param>
    protected async Task<(LanguageSummaryDto? Language, IActionResult? Redirect)> ResolveLanguageAsync(
        string langCode,
        string relativePath,
        CancellationToken ct)
    {
        var language = await LanguageService.GetByCodeAsync(langCode, ct);
        if (language is not null)
        {
            return (language, null);
        }

        var fallback = await LanguageService.GetDefaultAsync(ct);
        if (fallback is null)
        {
            TempData["Error"] = "Henüz aktif bir dil tanımlanmamış. Yönetim panelinden dil ekleyin.";
            return (null, Redirect("/hata/404"));
        }

        return (null, Redirect($"/{fallback.Code}{relativePath}"));
    }
}
