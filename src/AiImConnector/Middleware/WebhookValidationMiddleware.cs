namespace AiImConnector.Middleware;

/// <summary>
/// Webhook 驗證中介層 — 記錄所有 Webhook 請求的基本資訊
/// （各平台的簽名驗證在各自的 Controller 中處理）
/// </summary>
public class WebhookValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WebhookValidationMiddleware> _logger;

    public WebhookValidationMiddleware(RequestDelegate next, ILogger<WebhookValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api/webhook"))
        {
            _logger.LogInformation(
                "收到 Webhook 請求：{Method} {Path}，來源 IP：{RemoteIp}",
                context.Request.Method,
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            // 啟用 Request Body 緩衝，允許多次讀取
            context.Request.EnableBuffering();
        }

        await _next(context);
    }
}
