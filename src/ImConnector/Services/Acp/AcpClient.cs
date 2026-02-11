using AiImConnector.Configuration;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiImConnector.Services.Acp;

/// <summary>
/// Copilot SDK 客戶端實作 — 封裝 CopilotClient，支援連線到外部 ACP Server 或自動啟動 CLI。
/// 實作 IAsyncDisposable 確保資源正確釋放。
/// </summary>
public class CopilotClientService : ICopilotClientService
{
    private readonly AcpSettings _settings;
    private readonly ILogger<CopilotClientService> _logger;
    private CopilotClient? _client;

    public bool IsConnected => _client?.State == ConnectionState.Connected;

    public CopilotClientService(IOptions<AcpSettings> settings, ILogger<CopilotClientService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        var options = new CopilotClientOptions
        {
            Logger = _logger
        };

        // 優先使用外部 ACP Server（CliUrl），否則由 SDK 自動啟動 CLI
        if (!string.IsNullOrEmpty(_settings.CliUrl))
        {
            options.CliUrl = _settings.CliUrl;
            options.UseStdio = false;
            _logger.LogInformation("正在連線到外部 ACP Server：{CliUrl}...", _settings.CliUrl);
        }
        else
        {
            if (!string.IsNullOrEmpty(_settings.CliPath))
            {
                options.CliPath = _settings.CliPath;
            }
            _logger.LogInformation("正在啟動 Copilot CLI...");
        }

        _client = new CopilotClient(options);
        await _client.StartAsync();

        _logger.LogInformation("Copilot SDK 連線成功");
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (_client != null)
        {
            try
            {
                await _client.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Copilot SDK 停止時發生錯誤，嘗試強制停止");
                await _client.ForceStopAsync();
            }
            _logger.LogInformation("Copilot SDK 已停止");
        }
    }

    /// <inheritdoc />
    public async Task<CopilotSession> CreateSessionAsync(string? model = null, CancellationToken cancellationToken = default)
    {
        if (_client == null) throw new InvalidOperationException("Copilot SDK 尚未啟動");

        var config = new SessionConfig();
        if (!string.IsNullOrEmpty(model))
        {
            config.Model = model;
        }

        var session = await _client.CreateSessionAsync(config);
        _logger.LogInformation("建立 Session：{SessionId}，模型：{Model}", session.SessionId, model ?? "預設");
        return session;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
