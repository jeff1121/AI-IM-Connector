namespace AiImConnector.Services.Media;

/// <summary>
/// 多媒體暫存儲存介面 — 抽象化底層儲存機制（記憶體 / Redis），
/// 支援水平擴展與跨實例共享暫存資料。
/// </summary>
public interface IMediaStore
{
    /// <summary>儲存暫存多媒體（回傳是否成功）</summary>
    Task<bool> StoreAsync(string id, HostedMedia media);

    /// <summary>取得暫存多媒體（過期或不存在時回傳 null）</summary>
    Task<HostedMedia?> GetAsync(string id);

    /// <summary>移除暫存多媒體（回傳是否成功）</summary>
    Task<bool> RemoveAsync(string id);

    /// <summary>取得目前暫存數量</summary>
    Task<int> GetCountAsync();

    /// <summary>取得目前暫存總大小（bytes）</summary>
    Task<long> GetTotalBytesAsync();

    /// <summary>清理過期項目（回傳清理數量與釋放的 bytes）</summary>
    Task<(int Count, long FreedBytes)> CleanupExpiredAsync();
}
