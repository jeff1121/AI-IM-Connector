using System.Collections.Concurrent;
using AiImConnector.Configuration;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiImConnector.Services.Acp;

/// <summary>
/// Copilot Session 管理器 — 管理每個使用者對應的 CopilotSession 生命週期。
/// 使用 per-user SemaphoreSlim 鎖定確保執行緒安全，防止同一使用者並行建立多個 Session。
/// </summary>
public class CopilotSessionManager
{
    private readonly ICopilotClientService _clientService;
    private readonly AgentBindingSettings _bindingSettings;
    private readonly AcpSettings _acpSettings;
    private readonly ILogger<CopilotSessionManager> _logger;

    /// <summary>使用者對應的 CopilotSession（UserKey → CopilotSession）</summary>
    private readonly ConcurrentDictionary<string, CopilotSession> _sessions = new();

    /// <summary>建立 Session 時的鎖定物件，防止同一使用者並行建立多個 Session</summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _sessionLocks = new();

    public CopilotSessionManager(
        ICopilotClientService clientService,
        IOptions<AgentBindingSettings> bindingSettings,
        IOptions<AcpSettings> acpSettings,
        ILogger<CopilotSessionManager> logger)
    {
        _clientService = clientService;
        _bindingSettings = bindingSettings.Value;
        _acpSettings = acpSettings.Value;
        _logger = logger;
    }

    /// <summary>取得指定平台對應的 Agent 綁定設定</summary>
    public AgentBinding? GetBinding(string platform)
    {
        _bindingSettings.Bindings.TryGetValue(platform, out var binding);
        return binding;
    }

    /// <summary>取得或建立使用者的 CopilotSession（執行緒安全，防止同一使用者並行建立重複 Session）</summary>
    public async Task<CopilotSession> GetOrCreateSessionAsync(
        string platform,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var userKey = BuildSessionId(platform, userId);

        if (_sessions.TryGetValue(userKey, out var existingSession))
        {
            return existingSession;
        }

        // 使用 per-user lock 確保同一使用者不會並行建立多個 Session
        var sessionLock = _sessionLocks.GetOrAdd(userKey, _ => new SemaphoreSlim(1, 1));
        await sessionLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check：可能在等待 lock 期間已被其他執行緒建立
            if (_sessions.TryGetValue(userKey, out existingSession))
            {
                return existingSession;
            }

            var binding = GetBinding(platform);
            var model = binding?.Model;

            var session = await _clientService.CreateSessionAsync(model, cancellationToken);
            _sessions.TryAdd(userKey, session);
            _logger.LogInformation("建立新 Session：{UserKey} → {SessionId}", userKey, session.SessionId);
            return session;
        }
        finally
        {
            sessionLock.Release();
        }
    }

    /// <summary>發送訊息到 Copilot 並等待回應（含 stale session 自動重建）</summary>
    public async Task<string> SendMessageAsync(
        string platform,
        string userId,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var session = await GetOrCreateSessionAsync(platform, userId, cancellationToken);
        var binding = GetBinding(platform);
        var timeout = binding != null
            ? TimeSpan.FromSeconds(binding.ResponseTimeoutSeconds)
            : TimeSpan.FromSeconds(_acpSettings.ResponseTimeoutSeconds);

        _logger.LogDebug("發送 prompt 到 Session {SessionId}：{Prompt}",
            session.SessionId, prompt.Length > 100 ? prompt[..100] + "..." : prompt);

        try
        {
            // 使用 SDK 的 SendAndWaitAsync（含逾時控制）
            var response = await session.SendAndWaitAsync(new MessageOptions { Prompt = prompt }, timeout);

            var content = response?.Data?.Content ?? "";
            _logger.LogDebug("收到完整回應（{Length} 字元）", content.Length);
            return content;
        }
        catch (Exception ex) when (IsSessionNotFound(ex))
        {
            // Session 已失效（例如容器重啟後 Copilot CLI 子程序重新啟動），清除快取並重建
            _logger.LogWarning("Session {SessionId} 已失效，清除快取並重建新 Session", session.SessionId);
            var userKey = BuildSessionId(platform, userId);
            _sessions.TryRemove(userKey, out _);

            session = await GetOrCreateSessionAsync(platform, userId, cancellationToken);
            _logger.LogInformation("已重建 Session：{UserKey} → {SessionId}", userKey, session.SessionId);

            var response = await session.SendAndWaitAsync(new MessageOptions { Prompt = prompt }, timeout);
            var content = response?.Data?.Content ?? "";
            _logger.LogDebug("重建後收到回應（{Length} 字元）", content.Length);
            return content;
        }
    }

    /// <summary>判斷例外是否為 Session not found 錯誤</summary>
    private static bool IsSessionNotFound(Exception ex)
    {
        // 檢查整個例外鏈（含 InnerException）是否包含 "Session not found"
        var current = ex;
        while (current != null)
        {
            if (current.Message.Contains("Session not found", StringComparison.OrdinalIgnoreCase))
                return true;
            current = current.InnerException;
        }
        return false;
    }

    /// <summary>清除使用者的 Session</summary>
    public async Task ClearSessionAsync(string platform, string userId, CancellationToken cancellationToken = default)
    {
        var userKey = BuildSessionId(platform, userId);
        if (_sessions.TryRemove(userKey, out var session))
        {
            await session.DisposeAsync();
        }
        _logger.LogInformation("已清除 Session：{UserKey}", userKey);
    }

    /// <summary>產生使用者 Key</summary>
    public static string BuildSessionId(string platform, string userId) => $"{platform}:{userId}";
}
