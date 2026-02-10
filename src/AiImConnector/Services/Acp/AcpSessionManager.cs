using System.Collections.Concurrent;
using AiImConnector.Configuration;
using AiImConnector.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiImConnector.Services.Acp;

/// <summary>
/// ACP Session 管理器 — 管理每個使用者對應的 ACP Session 生命週期
/// </summary>
public class AcpSessionManager
{
    private readonly IAcpClient _acpClient;
    private readonly AgentBindingSettings _bindingSettings;
    private readonly ILogger<AcpSessionManager> _logger;

    /// <summary>已初始化的 ACP Server 記錄（避免重複初始化）</summary>
    private readonly ConcurrentDictionary<string, bool> _initializedServers = new();

    public AcpSessionManager(
        IAcpClient acpClient,
        IOptions<AgentBindingSettings> bindingSettings,
        ILogger<AcpSessionManager> logger)
    {
        _acpClient = acpClient;
        _bindingSettings = bindingSettings.Value;
        _logger = logger;
    }

    /// <summary>取得指定平台對應的 Agent 綁定設定</summary>
    public AgentBinding? GetBinding(string platform)
    {
        _bindingSettings.Bindings.TryGetValue(platform, out var binding);
        return binding;
    }

    /// <summary>確保 ACP Server 已初始化</summary>
    public async Task EnsureInitializedAsync(string serverUrl, CancellationToken cancellationToken = default)
    {
        if (_initializedServers.TryGetValue(serverUrl, out _))
            return;

        try
        {
            var response = await _acpClient.InitializeAsync(serverUrl, cancellationToken);
            if (response.IsSuccess)
            {
                _initializedServers.TryAdd(serverUrl, true);
                _logger.LogInformation("ACP Server 初始化成功：{ServerUrl}，能力：{Capabilities}",
                    serverUrl, response.Result?.Capabilities);
            }
            else
            {
                _logger.LogWarning("ACP Server 初始化失敗：{ServerUrl}，錯誤：{Error}",
                    serverUrl, response.Error?.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ACP Server 初始化例外：{ServerUrl}", serverUrl);
            throw;
        }
    }

    /// <summary>建立新的 ACP Session</summary>
    public async Task<string?> CreateSessionAsync(string serverUrl, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(serverUrl, cancellationToken);

        var response = await _acpClient.CreateSessionAsync(serverUrl, cancellationToken);
        if (response.IsSuccess)
        {
            _logger.LogInformation("ACP Session 建立成功：{SessionId}", response.Result?.SessionId);
            return response.Result?.SessionId;
        }

        _logger.LogError("ACP Session 建立失敗：{Error}", response.Error?.Message);
        return null;
    }

    /// <summary>發送訊息到 ACP Agent 並取得回應</summary>
    public async Task<string> SendMessageAsync(
        string platform,
        AcpPromptParams promptParams,
        CancellationToken cancellationToken = default)
    {
        var binding = GetBinding(platform)
            ?? throw new InvalidOperationException($"找不到平台 '{platform}' 的 Agent 綁定設定");

        await EnsureInitializedAsync(binding.AcpServerUrl, cancellationToken);
        return await _acpClient.SendPromptAsync(binding.AcpServerUrl, promptParams, cancellationToken);
    }
}
