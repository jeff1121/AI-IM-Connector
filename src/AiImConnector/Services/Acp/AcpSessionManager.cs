using System.Collections.Concurrent;
using AiImConnector.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiImConnector.Services.Acp;

/// <summary>
/// ACP Session 管理器 — 管理每個使用者對應的 ACP Session 生命週期
/// </summary>
public class CopilotSessionManager
{
    private readonly ICopilotClientService _clientService;
    private readonly AgentBindingSettings _bindingSettings;
    private readonly ILogger<CopilotSessionManager> _logger;

    /// <summary>使用者對應的 ACP Session ID（UserKey → AcpSessionId）</summary>
    private readonly ConcurrentDictionary<string, string> _sessions = new();

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

    /// <summary>取得或建立使用者的 ACP Session</summary>
    public async Task<string> GetOrCreateSessionAsync(
        string platform,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var userKey = BuildSessionId(platform, userId);

        // 嘗試從快取取得
        if (_sessions.TryGetValue(userKey, out var existingSessionId))
        {
            return existingSessionId;
        }

        // 建立新的 Session
        var sessionId = await _clientService.CreateSessionAsync(cancellationToken);
        _sessions.TryAdd(userKey, sessionId);
        _logger.LogInformation("建立新 Session：{UserKey} → {SessionId}", userKey, sessionId);
        return sessionId;
    }

    /// <summary>發送訊息到 ACP 並等待回應</summary>
    public async Task<string> SendMessageAsync(
        string platform,
        string userId,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var sessionId = await GetOrCreateSessionAsync(platform, userId, cancellationToken);
        var binding = GetBinding(platform);
        var model = binding?.Model;
        var timeout = binding != null
            ? TimeSpan.FromSeconds(binding.ResponseTimeoutSeconds)
            : TimeSpan.FromSeconds(120);

        return await _clientService.SendAndWaitAsync(sessionId, prompt, model, timeout, cancellationToken);
    }

    /// <summary>清除使用者的 Session</summary>
    public Task ClearSessionAsync(string platform, string userId, CancellationToken cancellationToken = default)
    {
        var userKey = BuildSessionId(platform, userId);
        _sessions.TryRemove(userKey, out _);
        _logger.LogInformation("已清除 Session：{UserKey}", userKey);
        return Task.CompletedTask;
    }

    /// <summary>產生使用者 Key</summary>
    public static string BuildSessionId(string platform, string userId) => $"{platform}:{userId}";
}
