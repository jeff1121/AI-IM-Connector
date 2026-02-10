using AiImConnector.Configuration;
using AiImConnector.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AiImConnector.Adapters.Line;

/// <summary>
/// LINE 適配器 — 負責與 LINE Messaging API 互動
/// </summary>
public class LineAdapter : IImAdapter
{
    public string PlatformName => "Line";

    private readonly HttpClient _httpClient;
    private readonly LineSettings _settings;
    private readonly ILogger<LineAdapter> _logger;
    private const string LineApiBaseUrl = "https://api.line.me/v2/bot";

    public LineAdapter(HttpClient httpClient, IOptions<LineSettings> settings, ILogger<LineAdapter> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ChannelAccessToken);
    }

    /// <inheritdoc />
    public async Task ReplyTextAsync(UnifiedMessage message, string replyText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(message.ReplyToken))
        {
            _logger.LogWarning("LINE 回覆缺少 ReplyToken，改用 Push 訊息");
            await PushTextAsync(message.UserId, replyText, cancellationToken);
            return;
        }

        var payload = new
        {
            replyToken = message.ReplyToken,
            messages = new[]
            {
                new { type = "text", text = TruncateText(replyText) }
            }
        };

        await PostToLineAsync($"{LineApiBaseUrl}/message/reply", payload, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReplyMediaAsync(UnifiedMessage message, MediaContent media, CancellationToken cancellationToken = default)
    {
        // LINE 需要提供公開 URL 來發送多媒體
        if (string.IsNullOrEmpty(media.SourceUrl))
        {
            _logger.LogWarning("LINE 多媒體回覆需要公開 URL，改為發送文字描述");
            await ReplyTextAsync(message, $"📎 [{media.Type}] {media.FileName ?? "多媒體檔案"}", cancellationToken);
            return;
        }

        var lineMessage = media.Type switch
        {
            MediaType.Image => new { type = "image", originalContentUrl = media.SourceUrl, previewImageUrl = media.ThumbnailUrl ?? media.SourceUrl } as object,
            MediaType.Video => new { type = "video", originalContentUrl = media.SourceUrl, previewImageUrl = media.ThumbnailUrl ?? "" },
            MediaType.Audio => new { type = "audio", originalContentUrl = media.SourceUrl, duration = 60000 },
            _ => new { type = "text", text = $"📎 檔案：{media.FileName ?? media.SourceUrl}" } as object
        };

        if (!string.IsNullOrEmpty(message.ReplyToken))
        {
            var payload = new { replyToken = message.ReplyToken, messages = new[] { lineMessage } };
            await PostToLineAsync($"{LineApiBaseUrl}/message/reply", payload, cancellationToken);
        }
        else
        {
            var payload = new { to = message.UserId, messages = new[] { lineMessage } };
            await PostToLineAsync($"{LineApiBaseUrl}/message/push", payload, cancellationToken);
        }
    }

    /// <summary>使用 Push API 主動發送文字訊息</summary>
    private async Task PushTextAsync(string userId, string text, CancellationToken cancellationToken)
    {
        var payload = new
        {
            to = userId,
            messages = new[]
            {
                new { type = "text", text = TruncateText(text) }
            }
        };

        await PostToLineAsync($"{LineApiBaseUrl}/message/push", payload, cancellationToken);
    }

    /// <summary>發送 POST 請求到 LINE API</summary>
    private async Task PostToLineAsync(string url, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("LINE API 呼叫失敗：{StatusCode} {Error}", response.StatusCode, error);
        }
    }

    /// <summary>截斷超過 LINE 限制的文字（5000 字元）</summary>
    private static string TruncateText(string text, int maxLength = 5000)
    {
        if (text.Length <= maxLength) return text;
        return text[..(maxLength - 3)] + "...";
    }
}
