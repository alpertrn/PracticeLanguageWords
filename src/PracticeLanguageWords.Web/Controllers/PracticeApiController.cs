using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Web.Services;

namespace PracticeLanguageWords.Web.Controllers;

/// <summary>
/// Kart akisinin AJAX ucnoktalari. Sayfa hic yenilenmez:
/// kart getir -> (zorluk sec | cevap yaz) -> sonraki kart.
/// </summary>
[Authorize]
[ApiController]
[Route("api/practice")]
public class PracticeApiController : ControllerBase
{
    private readonly IPracticeService _practiceService;
    private readonly ICurrentUserService _currentUser;
    private readonly IRecentWordsTracker _recentWords;

    public PracticeApiController(
        IPracticeService practiceService,
        ICurrentUserService currentUser,
        IRecentWordsTracker recentWords)
    {
        _practiceService = practiceService;
        _currentUser = currentUser;
        _recentWords = recentWords;
    }

    [HttpGet("next")]
    public async Task<IActionResult> Next(
        [FromQuery] PracticeSource source,
        [FromQuery] int? categoryId,
        [FromQuery] int? languageId,
        CancellationToken ct)
    {
        var scopeKey = BuildScopeKey(source, categoryId, languageId);
        var recent = _recentWords.Get(scopeKey);

        var card = await _practiceService.GetNextCardAsync(_currentUser.UserId, source, categoryId, languageId, recent, ct);

        if (card is null)
        {
            // Havuz bos: quiz modunda bilinmeyen kelime kalmadi / bugunku pay tukendi / kategoride kelime yok.
            var message = source == PracticeSource.UnknownOnly
                ? await _practiceService.GetUnknownFinishedMessageAsync(_currentUser.UserId, languageId, ct)
                : null;

            return Ok(new { finished = true, message });
        }

        _recentWords.Push(scopeKey, card.WordId);

        return Ok(new
        {
            finished = false,
            card.WordId,
            card.CategoryBadge,
            card.LanguageName,
            card.SpeechCode,
            card.TermText,
            card.TermRead,
            card.PronunciationEnabled,
            card.MasteryStreak,
            card.MasteryTarget,
            card.Answer
        });
    }

    /// <summary>Kategori pratigi: Kolay / Orta / Hiç Bilmiyorum.</summary>
    [HttpPost("rate")]
    public async Task<IActionResult> Rate([FromBody] RateRequest request, CancellationToken ct)
    {
        var result = await _practiceService.RateAsync(_currentUser.UserId, request, ct);
        return Ok(result);
    }

    /// <summary>Quiz modu: yazilan Turkce anlam.</summary>
    [HttpPost("answer")]
    public async Task<IActionResult> Answer([FromBody] AnswerRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.AnswerText))
        {
            return BadRequest(new { error = "Cevap boş olamaz." });
        }

        var result = await _practiceService.SubmitAnswerAsync(_currentUser.UserId, request, ct);
        return Ok(result);
    }

    /// <summary>Sesli telaffuz denemesi (tarayicinin konusma tanima ciktisi).</summary>
    [HttpPost("pronunciation")]
    public async Task<IActionResult> Pronunciation([FromBody] PronunciationRequest request, CancellationToken ct)
    {
        if (request.Transcripts is null || request.Transcripts.Count == 0)
        {
            return BadRequest(new { error = "Ses algılanamadı. Lütfen tekrar deneyin." });
        }

        var result = await _practiceService.SubmitPronunciationAsync(_currentUser.UserId, request, ct);
        return Ok(result);
    }

    /// <summary>"Artık kolay mı?" penceresinin cevabi.</summary>
    [HttpPost("mastery-decision")]
    public async Task<IActionResult> MasteryDecision([FromBody] MasteryDecisionRequest request, CancellationToken ct)
    {
        var result = await _practiceService.ApplyMasteryDecisionAsync(_currentUser.UserId, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Teknik bir mikrofon sorunu (izin yok, cihaz yok, destek yok, tekrarli sessizlik) yuzunden
    /// telaffuz adimi tamamlanamadi. Sayaci bozmadan kelimeyi bugun icin "gorundu" isaretler.
    /// </summary>
    [HttpPost("postpone")]
    public async Task<IActionResult> Postpone([FromBody] PostponeUnknownWordRequest request, CancellationToken ct)
    {
        await _practiceService.PostponeUnknownWordAsync(_currentUser.UserId, request, ct);
        return Ok(new { });
    }

    private string BuildScopeKey(PracticeSource source, int? categoryId, int? languageId) =>
        source == PracticeSource.UnknownOnly
            ? $"{_currentUser.UserId}:unknown:{languageId}"
            : $"{_currentUser.UserId}:category:{categoryId}";
}
