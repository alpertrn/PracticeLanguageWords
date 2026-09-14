using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Web.ViewModels;

namespace PracticeLanguageWords.Web.Controllers;

/// <summary>Giris/kayit sayfalari dilden bagimsizdir: /login, /register, /logout.</summary>
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILanguageService _languageService;

    public AccountController(IAuthService authService, ILanguageService languageService)
    {
        _authService = authService;
        _languageService = languageService;
    }

    [HttpGet("login")]
    public async Task<IActionResult> Login(string? returnUrl = null, CancellationToken ct = default)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return await RedirectToDefaultLanguageAsync(ct);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.LoginAsync(new LoginRequest(model.Username, model.Password), ct);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(model);
        }

        await SignInAsync(result);

        // Acik yonlendirme (open redirect) korumasi: sadece uygulama ici adreslere izin ver.
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return await RedirectToDefaultLanguageAsync(ct);
    }

    [HttpGet("register")]
    public async Task<IActionResult> Register(CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return await RedirectToDefaultLanguageAsync(ct);
        }

        return View(new RegisterViewModel());
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.RegisterAsync(
            new RegisterRequest(model.Username, model.Password, model.FirstName, model.LastName, model.Email), ct);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(model);
        }

        await SignInAsync(result);
        return await RedirectToDefaultLanguageAsync(ct);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return Redirect("/login");
    }

    private async Task<IActionResult> RedirectToDefaultLanguageAsync(CancellationToken ct)
    {
        var language = await _languageService.GetDefaultAsync(ct);
        return Redirect(language is null ? "/hata/404" : $"/{language.Code}");
    }

    private Task SignInAsync(AuthResult result)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId.ToString()),
            new(ClaimTypes.Name, result.Username),
            new(ClaimTypes.Role, result.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        return HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }
}
