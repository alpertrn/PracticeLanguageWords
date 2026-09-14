using System.Security.Claims;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public int UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }
    }

    public string Username => Principal?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public bool IsAdmin => Principal?.IsInRole("Admin") ?? false;
}
