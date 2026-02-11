using AiImConnector.Models;

namespace AiImConnector.Adapters.Line;

/// <summary>
/// LINE 訊息轉換器 — 負責 LINE 平台訊息與 UnifiedMessage 之間的轉換
/// </summary>
public static class LineMessageConverter
{
    /// <summary>將 LINE Webhook 事件轉換為 UnifiedMessage</summary>
    public static UnifiedMessage FromLineEvent(
        string eventType,
        string userId,
        string? replyToken,
        string? text,
        string? messageType,
        string? messageId)
    {
        var message = new UnifiedMessage
        {
            Platform = "Line",
            UserId = userId,
            ReplyToken = replyToken,
            Direction = MessageDirection.Incoming,
            Text = text
        };

        // 多媒體訊息
        if (messageType != null && messageType != "text" && !string.IsNullOrEmpty(messageId))
        {
            var mediaType = messageType switch
            {
                "image" => MediaType.Image,
                "video" => MediaType.Video,
                "audio" => MediaType.Audio,
                "file" => MediaType.File,
                _ => MediaType.File
            };

            var mimeType = messageType switch
            {
                "image" => "image/jpeg",
                "video" => "video/mp4",
                "audio" => "audio/m4a",
                _ => "application/octet-stream"
            };

            message.MediaContents.Add(new MediaContent
            {
                Type = mediaType,
                MimeType = mimeType,
                // LINE 多媒體需透過 API 下載，URL 在 Controller 中設定
                SourceUrl = $"https://api-data.line.me/v2/bot/message/{messageId}/content"
            });
        }

        return message;
    }
}
