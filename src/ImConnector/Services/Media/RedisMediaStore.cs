using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AiImConnector.Services.Media;

/// <summary>
/// Redis 多媒體暫存 — 使用 Redis 作為底層儲存，支援多實例部署與跨實例共享暫存資料。
/// 使用 Redis 原生 TTL 自動過期機制，無需額外清理邏輯。
/// Key 格式：media:{id}，Value：JSON 序列化的 HostedMedia。
/// </summary>
public class RedisMediaStore : IMediaStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisMediaStore> _logger;
    private const string KeyPrefix = "media:";
    private const string BytesCounterKey = "media:_total_bytes";

    public RedisMediaStore(IConnectionMultiplexer redis, ILogger<RedisMediaStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<bool> StoreAsync(string id, HostedMedia media)
    {
        try
        {
            var db = _redis.GetDatabase();
            var ttl = media.ExpiresAt - DateTimeOffset.UtcNow;
            if (ttl <= TimeSpan.Zero) return false;

            var entry = new RedisMediaEntry
            {
                Data = Convert.ToBase64String(media.Data),
                MimeType = media.MimeType,
                FileName = media.FileName,
                ExpiresAtUnix = media.ExpiresAt.ToUnixTimeMilliseconds()
            };

            var json = JsonSerializer.Serialize(entry);
            await db.StringSetAsync(KeyPrefix + id, json, ttl);
            await db.StringIncrementAsync(BytesCounterKey, media.Data.Length);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis 暫存多媒體失敗：{Id}", id);
            return false;
        }
    }

    public async Task<HostedMedia?> GetAsync(string id)
    {
        try
        {
            var db = _redis.GetDatabase();
            var json = await db.StringGetAsync(KeyPrefix + id);
            if (json.IsNullOrEmpty) return null;

            var entry = JsonSerializer.Deserialize<RedisMediaEntry>(json!);
            if (entry == null) return null;

            return new HostedMedia
            {
                Data = Convert.FromBase64String(entry.Data),
                MimeType = entry.MimeType,
                FileName = entry.FileName,
                ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(entry.ExpiresAtUnix)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis 取得多媒體失敗：{Id}", id);
            return null;
        }
    }

    public async Task<bool> RemoveAsync(string id)
    {
        try
        {
            var db = _redis.GetDatabase();

            // 取得資料大小以更新計數器
            var json = await db.StringGetAsync(KeyPrefix + id);
            if (!json.IsNullOrEmpty)
            {
                var entry = JsonSerializer.Deserialize<RedisMediaEntry>(json!);
                if (entry != null)
                {
                    var dataLength = (long)(entry.Data.Length * 3.0 / 4.0); // 粗估原始大小
                    await db.StringDecrementAsync(BytesCounterKey, dataLength);
                }
            }

            return await db.KeyDeleteAsync(KeyPrefix + id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis 移除多媒體失敗：{Id}", id);
            return false;
        }
    }

    public async Task<int> GetCountAsync()
    {
        try
        {
            var server = _redis.GetServers().FirstOrDefault();
            if (server == null) return 0;

            var count = 0;
            await foreach (var _ in server.KeysAsync(pattern: KeyPrefix + "*"))
            {
                if (!_.ToString().EndsWith("_total_bytes")) count++;
            }
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis 取得多媒體數量失敗");
            return 0;
        }
    }

    public async Task<long> GetTotalBytesAsync()
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(BytesCounterKey);
            return value.IsNullOrEmpty ? 0 : (long)value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis 取得暫存總大小失敗");
            return 0;
        }
    }

    /// <summary>Redis 使用原生 TTL 自動過期，無需手動清理</summary>
    public Task<(int Count, long FreedBytes)> CleanupExpiredAsync() => Task.FromResult((0, 0L));

    /// <summary>Redis 儲存用的序列化模型</summary>
    private class RedisMediaEntry
    {
        public string Data { get; set; } = "";
        public string MimeType { get; set; } = "application/octet-stream";
        public string? FileName { get; set; }
        public long ExpiresAtUnix { get; set; }
    }
}
