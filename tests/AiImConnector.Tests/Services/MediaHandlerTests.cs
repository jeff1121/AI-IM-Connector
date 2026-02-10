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
    public async Task ToAcpAttachmentAsync_有Base64資料_應正確轉換()
    {
        // Arrange
        var media = new MediaContent
        {
            Type = MediaType.Image,
            MimeType = "image/png",
            Base64Data = Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            FileName = "test.png"
        };

        // Act
        var attachment = await _handler.ToAcpAttachmentAsync(media);

        // Assert
        Assert.Equal("image", attachment.Type);
        Assert.Equal("image/png", attachment.MimeType);
        Assert.Equal("test.png", attachment.FileName);
        Assert.NotEmpty(attachment.Data);
    }

    [Fact]
    public async Task ToAcpAttachmentAsync_無Base64且有URL_應拋出例外()
    {
        // Arrange
        var media = new MediaContent
        {
            Type = MediaType.Image,
            SourceUrl = "https://example.com/image.png"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.ToAcpAttachmentAsync(media));
    }
}
