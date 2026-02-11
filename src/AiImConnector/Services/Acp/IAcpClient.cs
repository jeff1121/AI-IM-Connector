using GitHub.Copilot.SDK;

namespace AiImConnector.Services.Acp;

/// <summary>
/// Copilot SDK 客戶端介面 — 封裝 CopilotClient 的操作
/// </summary>
public interface ICopilotClientService : IAsyncDisposable
{
    /// <summary>啟動 Copilot CLI 程序</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>停止 Copilot CLI 程序</summary>
    Task StopAsync();

    /// <summary>建立新的 Session</summary>
    /// <param name="sessionId">自訂 Session ID（格式：{平台}:{使用者ID}）</param>
    /// <param name="model">AI 模型名稱（例如 gpt-5、claude-sonnet-4.5）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<CopilotSession> CreateSessionAsync(string sessionId, string model, CancellationToken cancellationToken = default);

    /// <summary>恢復已存在的 Session</summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<CopilotSession?> ResumeSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>發送訊息並等待完整回應</summary>
    /// <param name="session">Copilot Session</param>
    /// <param name="prompt">使用者訊息</param>
    /// <param name="timeout">逾時時間</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI 回應文字</returns>
    Task<string> SendAndWaitAsync(CopilotSession session, string prompt, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>刪除 Session</summary>
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>列出所有 Session</summary>
    Task<IReadOnlyList<SessionMetadata>> ListSessionsAsync(CancellationToken cancellationToken = default);
}
