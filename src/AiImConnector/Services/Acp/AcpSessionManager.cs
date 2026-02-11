using System.Collections.Concurrent;
using AiImConnector.Configuration;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiImConnector.Services.Acp;

/// <summary>
/// Copilot Session 管理器 — 管理每個使用者對應的 Copilot Session 生命週期
/// </summary>
public class CopilotSessionManager
{
    private readonly ICopilotClientService _clientService;
    private readonly AgentBindingSettings _bindingSettings;
    private readonly ILogger<CopilotSessionManager> _logger;

    /// <summary>活躍的 Session 快取（SessionId → CopilotSession）</summary>
    private readonly ConcurrentDictionary<string, CopilotSession> _sessions = new();

    public CopilotSessionManager(
        ICopilotClientService clientService,
        IOptions<AgentBindingSettings> bindingSettings,
        ILogger<CopilotSessionManager> logger)
    {
        _clientService = clientService;
        _bindingSettings = bindingSettings.Value;
        _logger = logger;
    }

    /// <summary>取得指定平台對應的 Agent 綁定設定</summary>
    public AgentBinding? GetBinding(string platform)
    {
        _bindingSettings.Bindings.TryGetValue(platform, out var binding);
        return binding;
    }

    /// <summary>取得或建立使用者的 Copilot Session</summary>
    public async Task<CopilotSession> GetOrCreateSessionAsync(
        string platform,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var sessionId = BuildSessionId(platform, userId);

        // 嘗試從快取取得
        if (_sessions.TryGetValue(sessionId, out var existingSession))
        {
            return existingSession;
        }

        // 嘗試恢復先前的 Session
        var resumed = await _clientService.ResumeSessionAsync(sessionId, cancellationToken);
        if (resumed != null)
        {
            _sessions.TryAdd(sessionId, resumed);
            _logger.LogInformation("恢復 Session 成功：{SessionId}", sessionId);
            return resumed;
        }

        // 建立新的 Session
        var binding = GetBinding(platform);
        var model = binding?.Model ?? "gpt-5";

        var session = await _clientService.CreateSessionAsync(sessionId, model, cancellationToken);
        _sessions.TryAdd(sessionId, session);
        _logger.LogInformation("建立新 Session：{SessionId}，模型：{Model}", sessionId, model);
        return session;
    }

    /// <summary>發送訊息到 Copilot 並等待回應</summary>
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
            : TimeSpan.FromSeconds(120);

        return await _clientService.SendAndWaitAsync(session, prompt, timeout, cancellationToken);
    }

    /// <summary>清除使用者的 Session</summary>
    public async Task ClearSessionAsync(string platform, string userId, CancellationToken cancellationToken = default)
    {
        var sessionId = BuildSessionId(platform, userId);

        if (_sessions.TryRemove(sessionId, out var session))
        {
            await session.DisposeAsync();
        }

        try
        {
            await _clientService.DeleteSessionAsync(sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "刪除 Session 時發生例外：{SessionId}", sessionId);
        }

        _logger.LogInformation("已清除 Session：{SessionId}", sessionId);
    }

    /// <summary>產生 Session ID</summary>
    public static string BuildSessionId(string platform, string userId) => $"{platform}:{userId}";
}
