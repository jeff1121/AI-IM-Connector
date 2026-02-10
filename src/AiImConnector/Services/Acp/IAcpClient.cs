using AiImConnector.Models;

namespace AiImConnector.Services.Acp;

/// <summary>
/// ACP 客戶端介面 — 定義與 ACP Server 通訊的操作
/// </summary>
public interface IAcpClient
{
    /// <summary>初始化與 ACP Server 的連線</summary>
    /// <param name="serverUrl">ACP Server URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>初始化回應（含 Agent 能力宣告）</returns>
    Task<AcpResponse> InitializeAsync(string serverUrl, CancellationToken cancellationToken = default);

    /// <summary>建立新的 ACP Session</summary>
    /// <param name="serverUrl">ACP Server URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>含 SessionId 的回應</returns>
    Task<AcpResponse> CreateSessionAsync(string serverUrl, CancellationToken cancellationToken = default);

    /// <summary>發送使用者訊息並取得 AI 回應</summary>
    /// <param name="serverUrl">ACP Server URL</param>
    /// <param name="promptParams">Prompt 參數（含上下文與附件）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI 回應文字</returns>
    Task<string> SendPromptAsync(string serverUrl, AcpPromptParams promptParams, CancellationToken cancellationToken = default);

    /// <summary>發送使用者訊息並以串流方式接收回應</summary>
    /// <param name="serverUrl">ACP Server URL</param>
    /// <param name="promptParams">Prompt 參數</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>串流回應的非同步列舉</returns>
    IAsyncEnumerable<AcpStreamEvent> SendPromptStreamAsync(string serverUrl, AcpPromptParams promptParams, CancellationToken cancellationToken = default);

    /// <summary>取消正在進行的請求</summary>
    /// <param name="serverUrl">ACP Server URL</param>
    /// <param name="sessionId">ACP Session ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task CancelAsync(string serverUrl, string sessionId, CancellationToken cancellationToken = default);
}
