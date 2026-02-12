using AiImConnector.Models;
using AiImConnector.Services;

namespace AiImConnector.Tests.Services;

public class AiResponseParserTests
{
    [Fact]
    public void Parse_PlainText_ReturnsTextOnly()
    {
        var result = AiResponseParser.Parse("這是一段純文字回應");

        Assert.Equal("這是一段純文字回應", result.Text);
        Assert.False(result.HasMedia);
        Assert.Empty(result.MediaContents);
    }

    [Fact]
    public void Parse_NullOrEmpty_ReturnsEmpty()
    {
        var result1 = AiResponseParser.Parse(null!);
        var result2 = AiResponseParser.Parse("");

        Assert.Equal("", result1.Text);
        Assert.False(result1.HasMedia);
        Assert.Equal("", result2.Text);
        Assert.False(result2.HasMedia);
    }

    [Fact]
    public void Parse_MarkdownImageWithUrl_ExtractsMedia()
    {
        var input = "這是你要的圖片：\n![可愛小馬](https://example.com/pony.png)\n希望你喜歡！";
        var result = AiResponseParser.Parse(input);

        Assert.Single(result.MediaContents);
        Assert.True(result.HasMedia);

        var media = result.MediaContents[0];
        Assert.Equal(MediaType.Image, media.Type);
        Assert.Equal("https://example.com/pony.png", media.SourceUrl);
        Assert.Equal("image/png", media.MimeType);
        Assert.Equal("可愛小馬", media.FileName);

        Assert.DoesNotContain("![", result.Text);
        Assert.Contains("希望你喜歡", result.Text);
    }

    [Fact]
    public void Parse_MarkdownImageWithJpeg_ExtractsCorrectMime()
    {
        var input = "![photo](https://example.com/photo.jpg)";
        var result = AiResponseParser.Parse(input);

        Assert.Single(result.MediaContents);
        Assert.Equal("image/jpeg", result.MediaContents[0].MimeType);
    }

    [Fact]
    public void Parse_MarkdownImageWithBase64DataUri_ExtractsBase64()
    {
        var base64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        var input = $"這是圖片：\n![小馬](data:image/png;base64,{base64})";
        var result = AiResponseParser.Parse(input);

        Assert.Single(result.MediaContents);
        var media = result.MediaContents[0];
        Assert.Equal(MediaType.Image, media.Type);
        Assert.Equal("image/png", media.MimeType);
        Assert.Equal(base64, media.Base64Data);
        Assert.Equal("小馬", media.FileName);
        Assert.Null(media.SourceUrl);
    }

    [Fact]
    public void Parse_StandaloneImageUrl_ExtractsMedia()
    {
        var input = "你可以在這裡下載圖片：\nhttps://cdn.example.com/images/pony.png\n完成！";
        var result = AiResponseParser.Parse(input);

        Assert.Single(result.MediaContents);
        var media = result.MediaContents[0];
        Assert.Equal("https://cdn.example.com/images/pony.png", media.SourceUrl);
        Assert.Equal("image/png", media.MimeType);
    }

    [Fact]
    public void Parse_StandaloneBase64DataUri_ExtractsMedia()
    {
        var base64 = "iVBORw0KGgo=";
        var input = $"這是圖片資料：data:image/jpeg;base64,{base64} 以上就是結果";
        var result = AiResponseParser.Parse(input);

        Assert.Single(result.MediaContents);
        var media = result.MediaContents[0];
        Assert.Equal("image/jpeg", media.MimeType);
        Assert.Equal(base64, media.Base64Data);
    }

    [Fact]
    public void Parse_MultipleImages_ExtractsAll()
    {
        var input = "這裡有兩張圖：\n![圖一](https://example.com/a.png)\n![圖二](https://example.com/b.jpg)";
        var result = AiResponseParser.Parse(input);

        Assert.Equal(2, result.MediaContents.Count);
        Assert.Equal("https://example.com/a.png", result.MediaContents[0].SourceUrl);
        Assert.Equal("https://example.com/b.jpg", result.MediaContents[1].SourceUrl);
    }

    [Fact]
    public void Parse_LocalFilePath_NotExtracted()
    {
        // 本地檔案路徑不應被擷取為多媒體
        var input = "檔案已存好在 `C:\\Users\\Jeff\\Downloads\\running-cute-pony.png`。";
        var result = AiResponseParser.Parse(input);

        Assert.False(result.HasMedia);
        Assert.Contains("pony.png", result.Text);
    }

    [Fact]
    public void Parse_WindowsFilePath_DetectedAsLocalPath()
    {
        var input = "圖片已經存好在 `C:\\Users\\Jeff.Hou\\Downloads\\running-cute-pony.png`。";
        var result = AiResponseParser.Parse(input);

        Assert.True(result.HasLocalFilePaths);
        Assert.Contains(result.LocalFilePaths,
            p => p.Contains("running-cute-pony.png"));
        Assert.False(result.HasMedia);
    }

    [Fact]
    public void Parse_UnixFilePath_DetectedAsLocalPath()
    {
        var input = "圖片儲存在 /Users/jeff/Downloads/image.png，請查收。";
        var result = AiResponseParser.Parse(input);

        Assert.True(result.HasLocalFilePaths);
        Assert.Contains(result.LocalFilePaths,
            p => p.Contains("image.png"));
    }

    [Fact]
    public void Parse_UrlNotDetectedAsLocalPath()
    {
        var input = "![img](https://example.com/image.png)";
        var result = AiResponseParser.Parse(input);

        Assert.False(result.HasLocalFilePaths);
        Assert.True(result.HasMedia);
    }

    [Fact]
    public void Parse_MixedUrlAndLocalPath()
    {
        var input = "這是圖片：\n![圖](https://example.com/a.png)\n另外也存在 C:\\Users\\Jeff\\out.png";
        var result = AiResponseParser.Parse(input);

        Assert.True(result.HasMedia);
        Assert.True(result.HasLocalFilePaths);
    }

    [Fact]
    public void Parse_NonImageMarkdownLink_NotExtracted()
    {
        // 一般 Markdown 連結（不含 !）不應被擷取
        var input = "[點此下載](https://example.com/pony.png)";
        var result = AiResponseParser.Parse(input);

        Assert.False(result.HasMedia);
        Assert.Contains("[點此下載]", result.Text);
    }

    [Fact]
    public void Parse_ImageUrlWithQueryParams_ExtractsMedia()
    {
        var input = "圖片在這：\nhttps://cdn.example.com/img.png?token=abc123";
        var result = AiResponseParser.Parse(input);

        Assert.Single(result.MediaContents);
        Assert.Equal("https://cdn.example.com/img.png?token=abc123", result.MediaContents[0].SourceUrl);
    }

    [Fact]
    public void Parse_CleanupExtraNewlines()
    {
        var input = "文字前\n\n\n![img](https://example.com/a.png)\n\n\n\n文字後";
        var result = AiResponseParser.Parse(input);

        // 確保多餘空行被清理
        Assert.DoesNotContain("\n\n\n", result.Text);
        Assert.Contains("文字前", result.Text);
        Assert.Contains("文字後", result.Text);
    }

    [Fact]
    public void Parse_MultiLineBase64_StripsWhitespace()
    {
        // AI 可能在 base64 字串中插入換行
        var cleanBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        var multiLineBase64 = "iVBORw0KGgoAAAANSUhEUg\nAAAAEAAAABCAYAAAAfFcSJ\nAAAADUlEQVR42mNk+M9QDw\nADhgGAWjR9awAAAABJRU5E\nrkJggg==";
        var input = $"![圖片](data:image/png;base64,{multiLineBase64})";
        var result = AiResponseParser.Parse(input);

        Assert.Single(result.MediaContents);
        Assert.Equal(cleanBase64, result.MediaContents[0].Base64Data);
    }

    [Fact]
    public void Parse_StandaloneMultiLineBase64_StripsWhitespace()
    {
        var cleanBase64 = "iVBORw0KGgo=";
        var input = "data:image/png;base64,iVBOR\n w0KGgo= 完成";
        var result = AiResponseParser.Parse(input);

        Assert.Single(result.MediaContents);
        Assert.Equal(cleanBase64, result.MediaContents[0].Base64Data);
    }
}
