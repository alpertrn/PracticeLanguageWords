using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Web.Controllers;

/// <summary>
/// Anasayfadaki haftalik lider tablosunun "Tüm kullanıcıları göster" butonu icin tek uçnokta.
/// İlk 5 zaten dashboard ile birlikte geliyor; bu uçnokta yalnizca butona basilinca cagrilir.
/// </summary>
[Authorize]
[ApiController]
[Route("api/leaderboard")]
public class LeaderboardApiController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly ICurrentUserService _currentUser;

    public LeaderboardApiController(ILeaderboardService leaderboardService, ICurrentUserService currentUser)
    {
        _leaderboardService = leaderboardService;
        _currentUser = currentUser;
    }

    [HttpGet("full")]
    public async Task<IActionResult> Full(CancellationToken ct)
    {
        var entries = await _leaderboardService.GetWeeklyFullListAsync(_currentUser.UserId, ct);
        return Ok(new { entries });
    }
}
