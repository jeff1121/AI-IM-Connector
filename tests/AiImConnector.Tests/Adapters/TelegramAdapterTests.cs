using AiImConnector.Adapters.Telegram;
using AiImConnector.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AiImConnector.Tests.Adapters;

/// <summary>
/// Telegram 訊息轉換器單元測試
/// </summary>
public class TelegramMessageConverterTests
{
    [Fact]
    public void FromTelegramUpdate_文字訊息_應正確轉換()
    {
        // Arrange
        var update = new Update
        {
            Message = new Message
            {
                From = new User { Id = 12345, FirstName = "Test" },
                Chat = new Chat { Id = 67890, Type = ChatType.Private },
                Text = "你好世界",
                Date = DateTime.UtcNow
            }
        };

        // Act
        var message = TelegramMessageConverter.FromTelegramUpdate(update);

        // Assert
        Assert.NotNull(message);
        Assert.Equal("Telegram", message!.Platform);
        Assert.Equal("12345", message.UserId);
        Assert.Equal("67890", message.ChatId);
        Assert.Equal("你好世界", message.Text);
        Assert.Empty(message.MediaContents);
    }

    [Fact]
    public void FromTelegramUpdate_無Message_應回傳null()
    {
        var update = new Update();
        var message = TelegramMessageConverter.FromTelegramUpdate(update);
        Assert.Null(message);
    }

    [Fact]
    public void FromTelegramUpdate_圖片訊息_應包含多媒體()
    {
        // Arrange
        var update = new Update
        {
            Message = new Message
            {
                From = new User { Id = 12345, FirstName = "Test" },
                Chat = new Chat { Id = 67890, Type = ChatType.Private },
                Photo = new[]
                {
                    new PhotoSize { FileId = "small-id", Width = 90, Height = 90 },
                    new PhotoSize { FileId = "large-id", Width = 800, Height = 600 }
                },
                Date = DateTime.UtcNow
            }
        };

        // Act
        var message = TelegramMessageConverter.FromTelegramUpdate(update);

        // Assert
        Assert.NotNull(message);
        Assert.Single(message!.MediaContents);
        Assert.Equal(MediaType.Image, message.MediaContents[0].Type);
        Assert.Equal("large-id", message.MediaContents[0].SourceUrl); // 應取最大尺寸
    }

    [Fact]
    public void FromTelegramUpdate_文件訊息_應包含檔案資訊()
    {
        var update = new Update
        {
            Message = new Message
            {
                From = new User { Id = 12345, FirstName = "Test" },
                Chat = new Chat { Id = 67890, Type = ChatType.Private },
                Document = new Document
                {
                    FileId = "doc-id",
                    FileName = "report.pdf",
                    MimeType = "application/pdf",
                    FileSize = 1024
                },
                Date = DateTime.UtcNow
            }
        };

        var message = TelegramMessageConverter.FromTelegramUpdate(update);

        Assert.NotNull(message);
        Assert.Single(message!.MediaContents);
        var media = message.MediaContents[0];
        Assert.Equal(MediaType.File, media.Type);
        Assert.Equal("report.pdf", media.FileName);
        Assert.Equal("application/pdf", media.MimeType);
    }
}
