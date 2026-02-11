using AiImConnector.Models;
using AiImConnector.Services.Media;
using Microsoft.Extensions.Logging.Abstractions;

namespace AiImConnector.Tests.Services;

/// <summary>
/// MediaHandler 單元測試（僅測試不需要網路的邏輯）
/// </summary>
public class MediaHandlerTests
{
    private readonly MediaHandler _handler;

    public MediaHandlerTests()
    {
        var httpClient = new HttpClient();
        var logger = new NullLogger<MediaHandler>();
        _handler = new MediaHandler(httpClient, logger);
    }

    [Theory]
    [InlineData("image/jpeg", MediaType.Image)]
    [InlineData("image/png", MediaType.Image)]
    [InlineData("image/gif", MediaType.Image)]
    [InlineData("video/mp4", MediaType.Video)]
    [InlineData("video/quicktime", MediaType.Video)]
    [InlineData("audio/mpeg", MediaType.Audio)]
    [InlineData("audio/ogg", MediaType.Audio)]
    [InlineData("application/pdf", MediaType.File)]
    [InlineData("application/octet-stream", MediaType.File)]
    public void DetectMediaType_應正確判斷多媒體類型(string mimeType, MediaType expected)
    {
        var result = _handler.DetectMediaType(mimeType);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToMediaDescription_圖片_應產生正確描述()
    {
        // Arrange
        var media = new MediaContent
        {
            Type = MediaType.Image,
            MimeType = "image/png",
            FileName = "test.png",
            FileSize = 1024
        };

        // Act
        var description = _handler.ToMediaDescription(media);

        // Assert
        Assert.Contains("圖片", description);
        Assert.Contains("test.png", description);
        Assert.Contains("1024", description);
    }

    [Fact]
    public void ToMediaDescription_無檔名_應使用MimeType()
    {
        // Arrange
        var media = new MediaContent
        {
            Type = MediaType.Audio,
            MimeType = "audio/mpeg"
        };

        // Act
        var description = _handler.ToMediaDescription(media);

        // Assert
        Assert.Contains("音訊", description);
        Assert.Contains("audio/mpeg", description);
    }
}
