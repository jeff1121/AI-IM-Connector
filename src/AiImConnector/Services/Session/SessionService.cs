using AiImConnector.Models;
using Microsoft.Extensions.Logging;

namespace AiImConnector.Services.Session;

/// <summary>
/// Session 服務 — 管理對話上下文的建立、取得與清除
/// </summary>
public class SessionService
{
    private readonly ISessionStore _store;
    private readonly ILogger<SessionService> _logger;

    /// <summary>Session 最大存活時間</summary>
    private static readonly TimeSpan SessionMaxAge = TimeSpan.FromMinutes(30);

    public SessionService(ISessionStore store, ILogger<SessionService> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>產生 Session ID</summary>
    public static string BuildSessionId(string platform, string userId) => $"{platform}:{userId}";

    /// <summary>取得或建立 Session</summary>
    public async Task<SessionContext> GetOrCreateAsync(string platform, string userId)
    {
        var sessionId = BuildSessionId(platform, userId);
        var session = await _store.GetAsync(sessionId);

        if (session != null)
        {
            session.Touch();
            await _store.SetAsync(session);
            return session;
        }

        session = new SessionContext
        {
            SessionId = sessionId,
            Platform = platform,
            UserId = userId
        };

        await _store.SetAsync(session);
        _logger.LogInformation("建立新的 Session：{SessionId}", sessionId);
        return session;
    }

    /// <summary>新增對話紀錄</summary>
    public async Task AddEntryAsync(string platform, string userId, string role, string content, List<string>? mediaDescriptions = null)
    {
        var session = await GetOrCreateAsync(platform, userId);
        session.History.Add(new ConversationEntry
        {
            Role = role,
            Content = content,
            MediaDescriptions = mediaDescriptions ?? new(),
            Timestamp = DateTimeOffset.UtcNow
        });
        session.Touch();
        await _store.SetAsync(session);
    }

    /// <summary>清除指定使用者的對話歷史</summary>
    public async Task ClearAsync(string platform, string userId)
    {
        var sessionId = BuildSessionId(platform, userId);
        await _store.RemoveAsync(sessionId);
        _logger.LogInformation("已清除 Session：{SessionId}", sessionId);
    }

    /// <summary>清除過期的 Session</summary>
    public async Task CleanupExpiredAsync()
    {
        await _store.CleanupExpiredAsync(SessionMaxAge);
        _logger.LogDebug("已清除過期的 Session（超過 {MaxAge} 分鐘）", SessionMaxAge.TotalMinutes);
    }
}
