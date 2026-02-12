using System.Collections.Concurrent;
using AiImConnector.Configuration;
using AiImConnector.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiImConnector.Services.Media;

/// <summary>
/// 多媒體暫存服務 — 將 Base64 多媒體資料暫存在記憶體中，並提供公開 URL 供 IM 平台存取。
/// 每筆暫存資料在 10 分鐘後自動過期，由背景 Timer 定期清理。
/// </summary>
public class MediaHostingService : IDisposable
{
    private readonly ConcurrentDictionary<string, HostedMedia> _store = new();
    private readonly string _publicBaseUrl;
    private readonly ILogger<MediaHostingService> _logger;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _mediaTtl = TimeSpan.FromMinutes(10);

    public MediaHostingService(IOptions<ConnectorSettings> settings, ILogger<MediaHostingService> logger)
    {
        _publicBaseUrl = (settings.Value.PublicBaseUrl ?? "").TrimEnd('/');
        _logger = logger;
        _cleanupTimer = new Timer(_ => CleanupExpired(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        if (string.IsNullOrEmpty(_publicBaseUrl))
        {
            _logger.LogWarning("Connector:PublicBaseUrl 未設定，Base64 多媒體將無法透過公開 URL 提供給 IM 平台");
        }
    }

    /// <summary>暫存 Base64 多媒體並回傳公開 URL</summary>
    public string? HostMedia(MediaContent media)
    {
        if (string.IsNullOrEmpty(media.Base64Data))
            return null;

        if (string.IsNullOrEmpty(_publicBaseUrl))
        {
            _logger.LogWarning("無法暫存多媒體：PublicBaseUrl 未設定");
            return null;
        }

        byte[] data;
        try
        {
            data = Convert.FromBase64String(media.Base64Data);
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "無效的 Base64 資料，無法暫存多媒體（長度={Length}）", media.Base64Data.Length);
            return null;
        }

        var id = Guid.NewGuid().ToString("N");
        _store[id] = new HostedMedia
        {
            Data = data,
            MimeType = media.MimeType,
            FileName = media.FileName,
            ExpiresAt = DateTimeOffset.UtcNow.Add(_mediaTtl)
        };

        var url = $"{_publicBaseUrl}/api/media/{id}";
        _logger.LogDebug("暫存多媒體 {Id}，過期時間：{ExpiresAt}，URL：{Url}", id, _store[id].ExpiresAt, url);
        return url;
    }

    /// <summary>取得暫存的多媒體資料</summary>
    public HostedMedia? GetMedia(string id)
    {
        if (_store.TryGetValue(id, out var media))
        {
            if (media.ExpiresAt > DateTimeOffset.UtcNow) return media;
            _store.TryRemove(id, out _);
        }
        return null;
    }

    private void CleanupExpired()
    {
        var count = 0;
        var now = DateTimeOffset.UtcNow;
        foreach (var kvp in _store)
        {
            if (kvp.Value.ExpiresAt <= now && _store.TryRemove(kvp.Key, out _))
                count++;
        }
        if (count > 0)
            _logger.LogDebug("已清理 {Count} 筆過期暫存多媒體", count);
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>暫存的多媒體資料</summary>
public class HostedMedia
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string MimeType { get; set; } = "application/octet-stream";
    public string? FileName { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
