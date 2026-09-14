using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Infrastructure.Security;

/// <summary>
/// Sifreler asla duz metin saklanmaz. BCrypt kendi salt'ini uretir ve hash icine gomer.
/// WorkFactor 12: 2025 sonrasi donanimlar icin makul bir maliyet/guvenlik dengesi.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string plainPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);

    public bool Verify(string plainPassword, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Bozuk/eski format hash: dogrulama basarisiz sayilir, uygulama cokmez.
            return false;
        }
    }
}
