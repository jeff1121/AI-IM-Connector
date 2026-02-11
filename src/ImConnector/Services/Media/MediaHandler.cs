using AiImConnector.Models;
using Microsoft.Extensions.Logging;

namespace AiImConnector.Services.Media;

/// <summary>
/// 多媒體處理實作 — 負責多媒體檔案的下載、轉換與類型判斷。
/// 包含 URL 驗證以防止 SSRF（Server-Side Request Forgery）攻擊。
/// </summary>
public class MediaHandler : IMediaHandler
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MediaHandler> _logger;

    /// <summary>允許下載多媒體的合法主機清單（僅限 IM 平台官方 API）</summary>
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "api-data.line.me",
        "api.line.me",
        "api.telegram.org"
    };

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
        // SSRF 防護：驗證 URL 是否為允許的 IM 平台主機
        ValidateUrl(url);

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
    public string ToMediaDescription(MediaContent media)
    {
        var typeName = media.Type switch
        {
            MediaType.Image => "圖片",
            MediaType.Video => "影片",
            MediaType.Audio => "音訊",
            MediaType.File => "檔案",
            _ => "多媒體內容"
        };

        var detail = media.FileName ?? media.MimeType;
        var size = media.FileSize > 0 ? $"，大小：{media.FileSize} bytes" : "";
        return $"[使用者傳送了一個{typeName}：{detail}{size}]";
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

    /// <summary>
    /// 驗證下載 URL 是否為允許的 IM 平台主機，防止 SSRF 攻擊。
    /// 僅允許 HTTPS 協定且主機必須在白名單中。
    /// </summary>
    private static void ValidateUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException($"無效的多媒體 URL 格式");

        if (uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException($"僅允許 HTTPS 協定下載多媒體");

        if (!AllowedHosts.Contains(uri.Host))
            throw new ArgumentException($"不允許從此主機下載多媒體");
    }
}
