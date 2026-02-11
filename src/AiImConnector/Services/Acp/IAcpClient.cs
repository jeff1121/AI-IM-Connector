namespace AiImConnector.Services.Acp;

/// <summary>
/// ACP 客戶端介面 — 透過 TCP 連線到 ACP Server
/// </summary>
public interface ICopilotClientService : IAsyncDisposable
{
    /// <summary>連線到 ACP Server 並初始化</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>中斷連線</summary>
    Task StopAsync();

    /// <summary>建立新的 Session</summary>
    /// <returns>ACP Server 分配的 Session ID</returns>
    Task<string> CreateSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>發送訊息並等待完整回應</summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="prompt">使用者訊息</param>
    /// <param name="model">AI 模型名稱</param>
    /// <param name="timeout">逾時時間</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI 回應文字</returns>
    Task<string> SendAndWaitAsync(string sessionId, string prompt, string? model = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>是否已連線</summary>
    bool IsConnected { get; }
}
