using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiImConnector.Configuration;
using AiImConnector.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;

namespace AiImConnector.Adapters.Telegram;

/// <summary>
/// Telegram Webhook 控制器 — 接收 Telegram Bot 的 Webhook 更新。
/// 支援 Secret Token 驗證（常數時間比較），未設定時拒絕所有請求（fail-closed）。
/// </summary>
[ApiController]
[Route("api/webhook/telegram")]
public class TelegramWebhookController : ControllerBase
{
    private readonly TelegramAdapter _telegramAdapter;
    private readonly MessageRouter _messageRouter;
    private readonly TelegramSettings _settings;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        TelegramAdapter telegramAdapter,
        MessageRouter messageRouter,
        IOptions<TelegramSettings> settings,
        ILogger<TelegramWebhookController> logger)
    {
        _telegramAdapter = telegramAdapter;
        _messageRouter = messageRouter;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>接收 Telegram Webhook 更新</summary>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        // Fail-closed：未設定 Secret Token 時拒絕所有請求
        if (string.IsNullOrEmpty(_settings.SecretToken))
        {
            _logger.LogError("Telegram SecretToken 未設定，拒絕所有 Webhook 請求");
            return StatusCode(503);
        }

        // 使用常數時間比較驗證 Secret Token，防止 timing attack
        var secretToken = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].FirstOrDefault() ?? "";
        var expected = Encoding.UTF8.GetBytes(_settings.SecretToken);
        var actual = Encoding.UTF8.GetBytes(secretToken);
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            _logger.LogWarning("Telegram Webhook Secret Token 驗證失敗");
            return Unauthorized();
        }

        if (update.Message == null)
        {
            return Ok();
        }

        var message = TelegramMessageConverter.FromTelegramUpdate(update);
        if (message == null)
        {
            return Ok();
        }

        // 解析多媒體的實際下載 URL
        foreach (var media in message.MediaContents)
        {
            if (!string.IsNullOrEmpty(media.SourceUrl) && !media.SourceUrl.StartsWith("http"))
            {
                // SourceUrl 目前存的是 FileId，需要轉換為實際 URL
                var fileUrl = await _telegramAdapter.GetFileUrlAsync(media.SourceUrl);
                media.SourceUrl = fileUrl;
            }
        }

        // 非同步處理訊息
        _ = Task.Run(async () =>
        {
            try
            {
                var response = await _messageRouter.RouteMessageAsync(message);

                // 發送文字回應
                if (!string.IsNullOrEmpty(response.Text))
                {
                    await _telegramAdapter.ReplyTextAsync(message, response.Text);
                }

                // 發送多媒體回應
                foreach (var media in response.MediaContents)
                {
                    await _telegramAdapter.ReplyMediaAsync(message, media);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理 Telegram 訊息失敗：{UserId}", message.UserId);
            }
        });

        return Ok();
    }
}
