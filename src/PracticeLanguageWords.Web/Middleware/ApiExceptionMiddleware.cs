using System.Text.Json;
using PracticeLanguageWords.Application.Common;

namespace PracticeLanguageWords.Web.Middleware;

/// <summary>
/// Merkezi hata yonetimi: AJAX/API istekleri icin JSON hata yaniti uretir,
/// normal sayfa istekleri icin hatayi yukari birakir (UseExceptionHandler devreye girer).
/// Beklenmeyen hatalar loglanir; kullaniciya teknik detay sizdirilmaz.
/// </summary>
public class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var isApiRequest = context.Request.Path.StartsWithSegments("/api")
                               || context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (!isApiRequest)
            {
                throw;
            }

            var (statusCode, message) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                BusinessRuleException => (StatusCodes.Status400BadRequest, ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata olustu. Lutfen tekrar deneyin.")
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Islenmemis hata: {Path}", context.Request.Path);
            }
            else
            {
                _logger.LogWarning("Is kurali hatasi: {Message} ({Path})", ex.Message, context.Request.Path);
            }

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }
}
