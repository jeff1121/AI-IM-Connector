using System.Net;
using System.Text.Json;

namespace AiImConnector.Middleware;

/// <summary>
/// 全域例外處理中介層 — 捕捉未處理的例外並回傳統一的錯誤格式。
/// 在非開發環境中隱藏內部錯誤細節，避免資訊洩漏。
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未處理的例外：{Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            ArgumentException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            InvalidOperationException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        // 僅在開發環境回傳詳細錯誤訊息，Production 環境隱藏所有內部細節
        var errorMessage = _environment.IsDevelopment()
            ? exception.Message
            : statusCode switch
            {
                HttpStatusCode.BadRequest => "請求格式無效，請檢查參數。",
                HttpStatusCode.Unauthorized => "驗證失敗，請確認身分資訊。",
                HttpStatusCode.InternalServerError => "伺服器內部錯誤，請稍後再試。",
                _ => "伺服器內部錯誤，請稍後再試。"
            };

        var response = _environment.IsDevelopment()
            ? new { error = new { message = errorMessage, type = exception.GetType().Name } } as object
            : new { error = new { message = errorMessage } };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
