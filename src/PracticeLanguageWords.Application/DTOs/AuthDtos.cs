using PracticeLanguageWords.Domain.Enums;

namespace PracticeLanguageWords.Application.DTOs;

public record RegisterRequest(string Username, string Password, string FirstName, string LastName, string Email);
public record LoginRequest(string Username, string Password);

public record AuthResult(bool Succeeded, string? ErrorMessage, int UserId, string Username, UserRole Role)
{
    public static AuthResult Fail(string message) => new(false, message, 0, string.Empty, UserRole.User);
    public static AuthResult Ok(int userId, string username, UserRole role) => new(true, null, userId, username, role);
}
