using AiImConnector.Configuration;
using AiImConnector.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AiImConnector.Adapters.Telegram;

/// <summary>
/// Telegram 適配器 — 負責與 Telegram Bot API 互動
/// </summary>
public class TelegramAdapter : IImAdapter
{
    public string PlatformName => "Telegram";

    private readonly TelegramBotClient _botClient;
    private readonly TelegramSettings _settings;
    private readonly ILogger<TelegramAdapter> _logger;

    public TelegramAdapter(IOptions<TelegramSettings> settings, ILogger<TelegramAdapter> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        _botClient = new TelegramBotClient(_settings.BotToken);
    }

    /// <inheritdoc />
    public async Task ReplyTextAsync(UnifiedMessage message, string replyText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(message.ChatId))
        {
            _logger.LogWarning("Telegram 回覆缺少 ChatId");
            return;
        }

        var chatId = new ChatId(long.Parse(message.ChatId));

        // Telegram 文字訊息限制 4096 字元
        if (replyText.Length > 4096)
        {
            // 分段發送
            for (var i = 0; i < replyText.Length; i += 4096)
            {
                var chunk = replyText.Substring(i, Math.Min(4096, replyText.Length - i));
                await _botClient.SendMessage(chatId, chunk, cancellationToken: cancellationToken);
            }
        }
        else
        {
            await _botClient.SendMessage(chatId, replyText, cancellationToken: cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task ReplyMediaAsync(UnifiedMessage message, MediaContent media, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(message.ChatId))
        {
            _logger.LogWarning("Telegram 多媒體回覆缺少 ChatId");
            return;
        }

        var chatId = new ChatId(long.Parse(message.ChatId));

        // 如果有 URL，直接用 URL 發送
        if (!string.IsNullOrEmpty(media.SourceUrl))
        {
            var inputFile = InputFile.FromUri(media.SourceUrl);
            switch (media.Type)
            {
                case MediaType.Image:
                    await _botClient.SendPhoto(chatId, inputFile, cancellationToken: cancellationToken);
                    break;
                case MediaType.Video:
                    await _botClient.SendVideo(chatId, inputFile, cancellationToken: cancellationToken);
                    break;
                case MediaType.Audio:
                    await _botClient.SendAudio(chatId, inputFile, cancellationToken: cancellationToken);
                    break;
                case MediaType.File:
                    await _botClient.SendDocument(chatId, inputFile, cancellationToken: cancellationToken);
                    break;
            }
        }
        else if (!string.IsNullOrEmpty(media.Base64Data))
        {
            // 從 Base64 建立串流發送
            var bytes = Convert.FromBase64String(media.Base64Data);
            using var stream = new MemoryStream(bytes);
            var inputFile = InputFile.FromStream(stream, media.FileName ?? "file");

            switch (media.Type)
            {
                case MediaType.Image:
                    await _botClient.SendPhoto(chatId, inputFile, cancellationToken: cancellationToken);
                    break;
                case MediaType.Video:
                    await _botClient.SendVideo(chatId, inputFile, cancellationToken: cancellationToken);
                    break;
                case MediaType.Audio:
                    await _botClient.SendAudio(chatId, inputFile, cancellationToken: cancellationToken);
                    break;
                case MediaType.File:
                    await _botClient.SendDocument(chatId, inputFile, cancellationToken: cancellationToken);
                    break;
            }
        }
    }

    /// <summary>透過 Telegram Bot API 取得檔案下載 URL</summary>
    public async Task<string?> GetFileUrlAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            var file = await _botClient.GetFile(fileId, cancellationToken);
            if (!string.IsNullOrEmpty(file.FilePath))
            {
                return $"https://api.telegram.org/file/bot{_settings.BotToken}/{file.FilePath}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得 Telegram 檔案 URL 失敗：{FileId}", fileId);
        }
        return null;
    }
}
