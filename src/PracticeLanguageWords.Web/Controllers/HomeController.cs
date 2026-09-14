using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Web.Controllers;

[Authorize]
public class HomeController : LanguageAwareController
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUser;

    public HomeController(
        IDashboardService dashboardService,
        ICurrentUserService currentUser,
        ILanguageService languageService) : base(languageService)
    {
        _dashboardService = dashboardService;
        _currentUser = currentUser;
    }

    /// <summary>Dil bazli ana sayfa: /en, /de, /fr ...</summary>
    [HttpGet("{langCode:alpha:length(2)}")]
    public async Task<IActionResult> Index(string langCode, CancellationToken ct)
    {
        var (language, redirect) = await ResolveLanguageAsync(langCode, string.Empty, ct);
        if (redirect is not null)
        {
            return redirect;
        }

        var dashboard = await _dashboardService.GetDashboardAsync(_currentUser.UserId, language!.Id, ct);
        return View(dashboard);
    }
}

[AllowAnonymous]
public class ErrorController : Controller
{
    [Route("hata")]
    [Route("hata/{statusCode:int}")]
    public IActionResult Index(int? statusCode = null)
    {
        ViewBag.StatusCode = statusCode ?? 500;
        ViewBag.Message = statusCode switch
        {
            404 => "Aradığınız sayfa bulunamadı.",
            403 => "Bu sayfaya erişim yetkiniz yok.",
            401 => "Bu sayfayı görmek için giriş yapmalısınız.",
            _ => "Beklenmeyen bir hata oluştu."
        };

        return View("Error");
    }
}
