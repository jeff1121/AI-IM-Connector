using AiImConnector.Adapters.Line;
using AiImConnector.Models;

namespace AiImConnector.Tests.Adapters;

/// <summary>
/// LINE 訊息轉換器單元測試
/// </summary>
public class LineMessageConverterTests
{
    [Fact]
    public void FromLineEvent_文字訊息_應正確轉換()
    {
        // Act
        var message = LineMessageConverter.FromLineEvent(
            eventType: "message",
            userId: "U1234567890",
            replyToken: "reply-token-123",
            text: "你好",
            messageType: "text",
            messageId: "msg-001");

        // Assert
        Assert.Equal("Line", message.Platform);
        Assert.Equal("U1234567890", message.UserId);
        Assert.Equal("reply-token-123", message.ReplyToken);
        Assert.Equal("你好", message.Text);
        Assert.Empty(message.MediaContents);
    }

    [Fact]
    public void FromLineEvent_圖片訊息_應包含多媒體資訊()
    {
        // Act
        var message = LineMessageConverter.FromLineEvent(
            eventType: "message",
            userId: "U1234567890",
            replyToken: "reply-token-456",
            text: null,
            messageType: "image",
            messageId: "msg-002");

        // Assert
        Assert.Single(message.MediaContents);
        var media = message.MediaContents[0];
        Assert.Equal(MediaType.Image, media.Type);
        Assert.Equal("image/jpeg", media.MimeType);
        Assert.Contains("msg-002", media.SourceUrl);
    }

    [Fact]
    public void FromLineEvent_影片訊息_應正確識別類型()
    {
        var message = LineMessageConverter.FromLineEvent(
            "message", "U123", "token", null, "video", "vid-001");

        Assert.Single(message.MediaContents);
        Assert.Equal(MediaType.Video, message.MediaContents[0].Type);
    }

    [Fact]
    public void FromLineEvent_指令訊息_IsCommand應為true()
    {
        var message = LineMessageConverter.FromLineEvent(
            "message", "U123", "token", "/clear", "text", "msg-003");

        Assert.True(message.IsCommand);
        Assert.Equal("clear", message.CommandName);
    }
}
