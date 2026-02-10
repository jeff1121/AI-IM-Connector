using AiImConnector.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AiImConnector.Adapters.Telegram;

/// <summary>
/// Telegram 訊息轉換器 — 負責 Telegram 訊息與 UnifiedMessage 之間的轉換
/// </summary>
public static class TelegramMessageConverter
{
    /// <summary>將 Telegram Update 轉換為 UnifiedMessage</summary>
    public static UnifiedMessage? FromTelegramUpdate(Update update)
    {
        var telegramMessage = update.Message;
        if (telegramMessage == null) return null;

        var message = new UnifiedMessage
        {
            Platform = "Telegram",
            UserId = telegramMessage.From?.Id.ToString() ?? string.Empty,
            ChatId = telegramMessage.Chat.Id.ToString(),
            Direction = MessageDirection.Incoming,
            Text = telegramMessage.Text ?? telegramMessage.Caption,
            Timestamp = telegramMessage.Date
        };

        // 處理多媒體
        if (telegramMessage.Photo is { Length: > 0 })
        {
            // 取最大尺寸的圖片
            var photo = telegramMessage.Photo.Last();
            message.MediaContents.Add(new MediaContent
            {
                Type = MediaType.Image,
                MimeType = "image/jpeg",
                SourceUrl = photo.FileId // 需透過 Telegram Bot API 取得實際 URL
            });
        }

        if (telegramMessage.Video != null)
        {
            message.MediaContents.Add(new MediaContent
            {
                Type = MediaType.Video,
                MimeType = telegramMessage.Video.MimeType ?? "video/mp4",
                SourceUrl = telegramMessage.Video.FileId,
                FileName = telegramMessage.Video.FileName,
                FileSize = telegramMessage.Video.FileSize
            });
        }

        if (telegramMessage.Audio != null)
        {
            message.MediaContents.Add(new MediaContent
            {
                Type = MediaType.Audio,
                MimeType = telegramMessage.Audio.MimeType ?? "audio/mpeg",
                SourceUrl = telegramMessage.Audio.FileId,
                FileName = telegramMessage.Audio.FileName,
                FileSize = telegramMessage.Audio.FileSize
            });
        }

        if (telegramMessage.Voice != null)
        {
            message.MediaContents.Add(new MediaContent
            {
                Type = MediaType.Audio,
                MimeType = telegramMessage.Voice.MimeType ?? "audio/ogg",
                SourceUrl = telegramMessage.Voice.FileId,
                FileSize = telegramMessage.Voice.FileSize
            });
        }

        if (telegramMessage.Document != null)
        {
            message.MediaContents.Add(new MediaContent
            {
                Type = MediaType.File,
                MimeType = telegramMessage.Document.MimeType ?? "application/octet-stream",
                SourceUrl = telegramMessage.Document.FileId,
                FileName = telegramMessage.Document.FileName,
                FileSize = telegramMessage.Document.FileSize
            });
        }

        return message;
    }
}
