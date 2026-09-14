using PracticeLanguageWords.Application.DTOs;

namespace PracticeLanguageWords.Application.Interfaces;

public interface IStreakService
{
    Task<StreakInfoDto> GetStreakAsync(int userId, CancellationToken ct = default);

    Task<IReadOnlyList<DayStatusDto>> GetLast7DaysAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Kullanicinin bugun bir kart uzerinde islem yaptigini kaydeder.
    /// Idempotenttir: ayni gun icinde kac kez cagrilirsa cagrilsin seriyi sadece bir kez artirir
    /// (UserStreakLog uzerindeki (UserId, ActivityDate) unique kisiti ve transaction ile korunur).
    /// Donen deger, bu cagrida "5'in kati" kilometre tasina ulasilip ulasilmadigini belirtir.
    /// </summary>
    Task<(StreakInfoDto Streak, bool MilestoneReached)> RegisterActivityAsync(int userId, CancellationToken ct = default);
}
