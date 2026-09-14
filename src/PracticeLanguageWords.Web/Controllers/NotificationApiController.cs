using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Web.Controllers;

/// <summary>
/// Tarayicinin Push API aboneligini (bkz. wwwroot/js/notifications.js) sunucuya kaydeder/siler.
/// Gunluk bildirim gonderimi burada degil, DailyNotificationBackgroundService icinde olur -
/// bu controller yalnizca abonelik CRUD'unu yonetir.
/// </summary>
[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationApiController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public NotificationApiController(INotificationService notificationService, ICurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint)
            || string.IsNullOrWhiteSpace(request.P256dh)
            || string.IsNullOrWhiteSpace(request.Auth))
        {
            return BadRequest(new { error = "Eksik abonelik bilgisi." });
        }

        await _notificationService.SubscribeAsync(_currentUser.UserId, request, ct);
        return Ok(new { });
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint))
        {
            return BadRequest(new { error = "Endpoint zorunludur." });
        }

        await _notificationService.UnsubscribeAsync(_currentUser.UserId, request.Endpoint, ct);
        return Ok(new { });
    }
}
