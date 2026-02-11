using AiImConnector.Configuration;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiImConnector.Services.Acp;

/// <summary>
/// Copilot SDK 客戶端實作 — 透過 Copilot SDK 啟動本機 Copilot CLI 並以 ACP 協定通訊
/// </summary>
public class CopilotClientService : ICopilotClientService
{
    private readonly CopilotClient _client;
    private readonly AcpSettings _settings;
    private readonly ILogger<CopilotClientService> _logger;
    private bool _started;

    public CopilotClientService(IOptions<AcpSettings> settings, ILogger<CopilotClientService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var options = new CopilotClientOptions
        {
            AutoStart = false,
            AutoRestart = true
        };

        // 若有指定 CLI 路徑，則設定
        if (!string.IsNullOrEmpty(_settings.CliPath))
        {
            options.CliPath = _settings.CliPath;
        }

        _client = new CopilotClient(options);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started) return;

        _logger.LogInformation("正在啟動 Copilot CLI...");
        await _client.StartAsync(cancellationToken);
        _started = true;
        _logger.LogInformation("Copilot CLI 啟動完成，連線狀態：{State}", _client.State);
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (!_started) return;

        _logger.LogInformation("正在停止 Copilot CLI...");
        await _client.StopAsync();
        _started = false;
        _logger.LogInformation("Copilot CLI 已停止");
    }

    /// <inheritdoc />
    public async Task<CopilotSession> CreateSessionAsync(string sessionId, string model, CancellationToken cancellationToken = default)
    {
        await EnsureStartedAsync(cancellationToken);

        var config = new SessionConfig
        {
            SessionId = sessionId,
            Model = model
        };

        _logger.LogInformation("建立 Copilot Session：{SessionId}，模型：{Model}", sessionId, model);
        var session = await _client.CreateSessionAsync(config, cancellationToken);
        return session;
    }

    /// <inheritdoc />
    public async Task<CopilotSession?> ResumeSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureStartedAsync(cancellationToken);

        try
        {
            _logger.LogInformation("恢復 Copilot Session：{SessionId}", sessionId);
            var session = await _client.ResumeSessionAsync(sessionId, cancellationToken: cancellationToken);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "恢復 Session 失敗（可能不存在）：{SessionId}", sessionId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string> SendAndWaitAsync(CopilotSession session, string prompt, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(_settings.ResponseTimeoutSeconds);

        _logger.LogDebug("發送訊息到 Session {SessionId}：{Prompt}", session.SessionId, prompt.Length > 100 ? prompt[..100] + "..." : prompt);

        var response = await session.SendAndWaitAsync(
            new MessageOptions { Prompt = prompt },
            effectiveTimeout,
            cancellationToken);

        var content = response?.Data?.Content ?? string.Empty;
        _logger.LogDebug("收到回應（{Length} 字元）", content.Length);
        return content;
    }

    /// <inheritdoc />
    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await EnsureStartedAsync(cancellationToken);
        await _client.DeleteSessionAsync(sessionId, cancellationToken);
        _logger.LogInformation("已刪除 Session：{SessionId}", sessionId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SessionMetadata>> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureStartedAsync(cancellationToken);
        return await _client.ListSessionsAsync(cancellationToken);
    }

    /// <summary>確保 Copilot CLI 已啟動</summary>
    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (!_started)
        {
            await StartAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
