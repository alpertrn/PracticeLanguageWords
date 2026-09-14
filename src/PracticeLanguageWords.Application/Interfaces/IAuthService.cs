using PracticeLanguageWords.Application.DTOs;

namespace PracticeLanguageWords.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default);
}

/// <summary>
/// Sifre hashleme/dogrulama detayini (BCrypt) Application katmanindan soyutlar.
/// Implementasyon Infrastructure katmaninda (SOLID - Dependency Inversion).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string hash);
}
