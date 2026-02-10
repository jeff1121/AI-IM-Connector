using System.Collections.Concurrent;
using AiImConnector.Models;

namespace AiImConnector.Services.Session;

/// <summary>
/// 記憶體 Session 儲存 — 使用 ConcurrentDictionary 實作
/// </summary>
public class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, SessionContext> _sessions = new();

    /// <inheritdoc />
    public Task<SessionContext?> GetAsync(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    /// <inheritdoc />
    public Task SetAsync(SessionContext session)
    {
        _sessions.AddOrUpdate(session.SessionId, session, (_, _) => session);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CleanupExpiredAsync(TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        var expiredKeys = _sessions
            .Where(kvp => kvp.Value.LastActiveAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _sessions.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
