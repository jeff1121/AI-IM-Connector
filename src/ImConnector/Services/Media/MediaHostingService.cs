using System.Security.Cryptography;
using AiImConnector.Configuration;
using AiImConnector.Models;
using AiImConnector.Telemetry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiImConnector.Services.Media;

/// <summary>
/// 多媒體暫存服務 — 將 Base64 多媒體資料暫存並提供公開 URL 供 IM 平台存取。
/// 底層儲存透過 IMediaStore 抽象化（記憶體 / Redis），支援水平擴展。
/// 設有最大暫存數量（1000 筆）與總容量上限（500 MB），防止資源耗盡。
/// </summary>
public class MediaHostingService : IDisposable
{
    private readonly IMediaStore _store;
    private readonly string _publicBaseUrl;
    private readonly ConnectorMetrics _metrics;
    private readonly ILogger<MediaHostingService> _logger;
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _mediaTtl = TimeSpan.FromMinutes(10);
    private readonly object _capacityLock = new();

    /// <summary>最大暫存數量</summary>
    private const int MaxEntries = 1000;
    /// <summary>總容量上限（500 MB）</summary>
    private const long MaxTotalBytes = 500 * 1024 * 1024;

    public MediaHostingService(
        IMediaStore store,
        IOptions<ConnectorSettings> settings,
        ConnectorMetrics metrics,
        ILogger<MediaHostingService> logger)
    {
        _store = store;
        _publicBaseUrl = (settings.Value.PublicBaseUrl ?? "").TrimEnd('/');
        _metrics = metrics;
        _logger = logger;
        _cleanupTimer = new Timer(_ => _ = CleanupExpiredAsync(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

        if (string.IsNullOrEmpty(_publicBaseUrl))
        {
            _logger.LogWarning("Connector:PublicBaseUrl 未設定，Base64 多媒體將無法透過公開 URL 提供給 IM 平台");
        }
    }

    /// <summary>暫存 Base64 多媒體並回傳公開 URL（含數量與容量上限檢查）</summary>
    public async Task<string?> HostMediaAsync(MediaContent media)
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

        // 使用 lock 確保容量檢查與新增為原子操作，防止 TOCTOU 競態條件
        string id;
        lock (_capacityLock)
        {
            var currentCount = _store.GetCountAsync().GetAwaiter().GetResult();
            var currentBytes = _store.GetTotalBytesAsync().GetAwaiter().GetResult();

            if (currentCount >= MaxEntries)
            {
                _logger.LogWarning("多媒體暫存已達上限（{MaxEntries} 筆），拒絕新增", MaxEntries);
                return null;
            }
            if (currentBytes + data.Length > MaxTotalBytes)
            {
                _logger.LogWarning("多媒體暫存總容量已達上限（{MaxTotalBytes} bytes），拒絕新增", MaxTotalBytes);
                return null;
            }

            // 使用加密安全隨機數產生 ID（比 GUID 更難猜測）
            var idBytes = RandomNumberGenerator.GetBytes(16);
            id = Convert.ToHexString(idBytes).ToLowerInvariant();

            var hostedMedia = new HostedMedia
            {
                Data = data,
                MimeType = media.MimeType,
                FileName = media.FileName,
                ExpiresAt = DateTimeOffset.UtcNow.Add(_mediaTtl)
            };

            _store.StoreAsync(id, hostedMedia).GetAwaiter().GetResult();
        }

        var url = $"{_publicBaseUrl}/api/media/{id}";
        _metrics.RecordMediaHosted();
        _logger.LogDebug("暫存多媒體 {Id}，過期時間：{ExpiresAt}，URL：{Url}", id, DateTimeOffset.UtcNow.Add(_mediaTtl), url);
        return url;
    }

    /// <summary>同步版本 — 維持向後相容（內部委派至非同步方法）</summary>
    public string? HostMedia(MediaContent media) => HostMediaAsync(media).GetAwaiter().GetResult();

    /// <summary>取得暫存的多媒體資料</summary>
    public async Task<HostedMedia?> GetMediaAsync(string id) => await _store.GetAsync(id);

    /// <summary>同步版本 — 維持向後相容</summary>
    public HostedMedia? GetMedia(string id) => _store.GetAsync(id).GetAwaiter().GetResult();

    /// <summary>取得暫存服務統計資訊</summary>
    public MediaHostingStats GetStats() => new()
    {
        EntryCount = _store.GetCountAsync().GetAwaiter().GetResult(),
        TotalBytes = _store.GetTotalBytesAsync().GetAwaiter().GetResult(),
        MaxEntries = MaxEntries,
        MaxTotalBytes = MaxTotalBytes
    };

    private async Task CleanupExpiredAsync()
    {
        var (count, freedBytes) = await _store.CleanupExpiredAsync();
        if (count > 0)
        {
            _metrics.RecordMediaExpired(count);
            _logger.LogDebug("已清理 {Count} 筆過期暫存多媒體，釋放 {Bytes} bytes", count, freedBytes);
        }
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

/// <summary>暫存服務統計資訊</summary>
public class MediaHostingStats
{
    public int EntryCount { get; set; }
    public long TotalBytes { get; set; }
    public int MaxEntries { get; set; }
    public long MaxTotalBytes { get; set; }
}
