namespace PracticeLanguageWords.Application.Interfaces;

/// <summary>
/// Web katmanindaki HttpContext/ClaimsPrincipal detayini Application katmanindan gizler.
/// Boylece servisler dogrudan HttpContext'e bagli olmaz, test edilebilir kalir.
/// </summary>
public interface ICurrentUserService
{
    int UserId { get; }
    string Username { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
}
