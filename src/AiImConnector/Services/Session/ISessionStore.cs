using AiImConnector.Models;

namespace AiImConnector.Services.Session;

/// <summary>
/// Session 儲存介面
/// </summary>
public interface ISessionStore
{
    /// <summary>取得 Session</summary>
    Task<SessionContext?> GetAsync(string sessionId);

    /// <summary>建立或更新 Session</summary>
    Task SetAsync(SessionContext session);

    /// <summary>刪除 Session</summary>
    Task RemoveAsync(string sessionId);

    /// <summary>清除過期的 Session</summary>
    Task CleanupExpiredAsync(TimeSpan maxAge);
}
