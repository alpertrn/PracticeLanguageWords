using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Domain.Entities;
using PracticeLanguageWords.Domain.Enums;
using PracticeLanguageWords.Domain.Interfaces;

namespace PracticeLanguageWords.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUnitOfWork uow, IPasswordHasher passwordHasher)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim();

        if (username.Length < 3)
        {
            return AuthResult.Fail("Kullanici adi en az 3 karakter olmalidir.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return AuthResult.Fail("Sifre en az 6 karakter olmalidir.");
        }

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var email = request.Email.Trim();

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return AuthResult.Fail("Ad ve soyad zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return AuthResult.Fail("E-posta zorunludur.");
        }

        if (await _uow.Users.UsernameExistsAsync(username, ct))
        {
            return AuthResult.Fail("Bu kullanici adi zaten kullaniliyor.");
        }

        var user = new User
        {
            Username = username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };

        _uow.Users.Add(user);
        await _uow.SaveChangesAsync(ct);

        // Yeni kullanici icin bos bir streak kaydi olusturulur; boylece dashboard sorgusu
        // her zaman UserStreak bulur, null-check karmasasi olusmaz.
        _uow.Streaks.Add(new UserStreak { UserId = user.Id, CurrentStreak = 0, LongestStreak = 0 });
        await _uow.SaveChangesAsync(ct);

        return AuthResult.Ok(user.Id, user.Username, user.Role);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByUsernameAsync(request.Username.Trim(), ct);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return AuthResult.Fail("Kullanici adi veya sifre hatali.");
        }

        return AuthResult.Ok(user.Id, user.Username, user.Role);
    }
}
