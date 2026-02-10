using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiImConnector.Configuration;
using AiImConnector.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AiImConnector.Adapters.Line;

/// <summary>
/// LINE Webhook 控制器 — 接收 LINE 平台的 Webhook 事件
/// </summary>
[ApiController]
[Route("api/webhook/line")]
public class LineWebhookController : ControllerBase
{
    private readonly LineAdapter _lineAdapter;
    private readonly MessageRouter _messageRouter;
    private readonly LineSettings _settings;
    private readonly ILogger<LineWebhookController> _logger;

    public LineWebhookController(
        LineAdapter lineAdapter,
        MessageRouter messageRouter,
        IOptions<LineSettings> settings,
        ILogger<LineWebhookController> logger)
    {
        _lineAdapter = lineAdapter;
        _messageRouter = messageRouter;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>接收 LINE Webhook 事件</summary>
    [HttpPost]
    public async Task<IActionResult> Post()
    {
        // 讀取請求 Body
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        // 驗證簽名
        var signature = Request.Headers["X-Line-Signature"].FirstOrDefault();
        if (!ValidateSignature(body, signature))
        {
            _logger.LogWarning("LINE Webhook 簽名驗證失敗");
            return Unauthorized();
        }

        // 解析事件
        try
        {
            using var doc = JsonDocument.Parse(body);
            var events = doc.RootElement.GetProperty("events");

            foreach (var evt in events.EnumerateArray())
            {
                var eventType = evt.GetProperty("type").GetString();
                if (eventType != "message") continue;

                var source = evt.GetProperty("source");
                var userId = source.GetProperty("userId").GetString() ?? string.Empty;
                var replyToken = evt.GetProperty("replyToken").GetString();
                var messageObj = evt.GetProperty("message");
                var messageType = messageObj.GetProperty("type").GetString();
                var messageId = messageObj.GetProperty("id").GetString();
                var text = messageType == "text" ? messageObj.GetProperty("text").GetString() : null;

                var message = LineMessageConverter.FromLineEvent(
                    eventType, userId, replyToken, text, messageType, messageId);

                // 非同步處理訊息（不阻塞 Webhook 回應）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var response = await _messageRouter.RouteMessageAsync(message);
                        await _lineAdapter.ReplyTextAsync(message, response);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "處理 LINE 訊息失敗：{UserId}", userId);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析 LINE Webhook 事件失敗");
            return BadRequest();
        }

        return Ok();
    }

    /// <summary>驗證 LINE Webhook 簽名</summary>
    private bool ValidateSignature(string body, string? signature)
    {
        if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(_settings.ChannelSecret))
            return false;

        var key = Encoding.UTF8.GetBytes(_settings.ChannelSecret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(bodyBytes);
        var computed = Convert.ToBase64String(hash);

        return computed == signature;
    }
}
