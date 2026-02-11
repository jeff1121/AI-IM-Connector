using GitHub.Copilot.SDK;

namespace AiImConnector.Services.Acp;

/// <summary>
/// Copilot SDK 客戶端服務介面 — 封裝 CopilotClient 生命週期與 Session 管理
/// </summary>
public interface ICopilotClientService : IAsyncDisposable
{
    /// <summary>啟動 Copilot 客戶端（連線到 ACP Server 或啟動 CLI）</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>停止 Copilot 客戶端</summary>
    Task StopAsync();

    /// <summary>建立新的 Session</summary>
    /// <param name="model">AI 模型名稱</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>SDK CopilotSession 物件</returns>
    Task<CopilotSession> CreateSessionAsync(string? model = null, CancellationToken cancellationToken = default);

    /// <summary>是否已連線</summary>
    bool IsConnected { get; }
}
