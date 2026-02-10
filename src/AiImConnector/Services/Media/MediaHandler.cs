using AiImConnector.Models;
using Microsoft.Extensions.Logging;

namespace AiImConnector.Services.Media;

/// <summary>
/// 多媒體處理實作 — 負責多媒體檔案的下載、轉換與類型判斷
/// </summary>
public class MediaHandler : IMediaHandler
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MediaHandler> _logger;

    public MediaHandler(HttpClient httpClient, ILogger<MediaHandler> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MediaContent> DownloadAsBase64Async(
        string url,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("正在下載多媒體檔案：{Url}", url);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (headers != null)
        {
            foreach (var (key, value) in headers)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var mimeType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"');

        var media = new MediaContent
        {
            Type = DetectMediaType(mimeType),
            SourceUrl = url,
            Base64Data = Convert.ToBase64String(bytes),
            MimeType = mimeType,
            FileName = fileName,
            FileSize = bytes.Length
        };

        _logger.LogInformation("多媒體下載完成：{MimeType}，大小：{Size} bytes", mimeType, bytes.Length);
        return media;
    }

    /// <inheritdoc />
    public Task<AcpAttachment> ToAcpAttachmentAsync(MediaContent media, CancellationToken cancellationToken = default)
    {
        // 如果沒有 Base64 資料，需要先下載
        if (string.IsNullOrEmpty(media.Base64Data) && !string.IsNullOrEmpty(media.SourceUrl))
        {
            throw new InvalidOperationException("多媒體內容尚未下載，請先呼叫 DownloadAsBase64Async");
        }

        var attachment = new AcpAttachment
        {
            Type = media.Type.ToString().ToLowerInvariant(),
            MimeType = media.MimeType,
            Data = media.Base64Data ?? string.Empty,
            FileName = media.FileName
        };

        return Task.FromResult(attachment);
    }

    /// <inheritdoc />
    public MediaType DetectMediaType(string mimeType)
    {
        return mimeType.ToLowerInvariant() switch
        {
            var m when m.StartsWith("image/") => MediaType.Image,
            var m when m.StartsWith("video/") => MediaType.Video,
            var m when m.StartsWith("audio/") => MediaType.Audio,
            _ => MediaType.File
        };
    }
}
