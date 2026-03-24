using System.Collections.Concurrent;

namespace AiImConnector.Services.Media;

/// <summary>
/// 記憶體內多媒體暫存 — 使用 ConcurrentDictionary 實作，適用於單一實例部署。
/// 應用程式重啟後暫存資料將遺失。
/// </summary>
public class InMemoryMediaStore : IMediaStore
{
    private readonly ConcurrentDictionary<string, HostedMedia> _store = new();
    private long _totalBytes;

    public Task<bool> StoreAsync(string id, HostedMedia media)
    {
        _store[id] = media;
        Interlocked.Add(ref _totalBytes, media.Data.Length);
        return Task.FromResult(true);
    }

    public Task<HostedMedia?> GetAsync(string id)
    {
        if (_store.TryGetValue(id, out var media))
        {
            if (media.ExpiresAt > DateTimeOffset.UtcNow) return Task.FromResult<HostedMedia?>(media);
            _store.TryRemove(id, out var removed);
            if (removed != null) Interlocked.Add(ref _totalBytes, -removed.Data.Length);
        }
        return Task.FromResult<HostedMedia?>(null);
    }

    public Task<bool> RemoveAsync(string id)
    {
        if (_store.TryRemove(id, out var removed))
        {
            Interlocked.Add(ref _totalBytes, -removed.Data.Length);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<int> GetCountAsync() => Task.FromResult(_store.Count);

    public Task<long> GetTotalBytesAsync() => Task.FromResult(Interlocked.Read(ref _totalBytes));

    public Task<(int Count, long FreedBytes)> CleanupExpiredAsync()
    {
        var count = 0;
        long freedBytes = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var kvp in _store)
        {
            if (kvp.Value.ExpiresAt <= now && _store.TryRemove(kvp.Key, out var removed))
            {
                freedBytes += removed.Data.Length;
                count++;
            }
        }

        if (count > 0) Interlocked.Add(ref _totalBytes, -freedBytes);
        return Task.FromResult((count, freedBytes));
    }
}
