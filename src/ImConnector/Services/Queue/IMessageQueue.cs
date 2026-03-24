using AiImConnector.Models;

namespace AiImConnector.Services.Queue;

/// <summary>
/// 訊息佇列介面 — 解耦 Webhook 接收與訊息處理，
/// 讓 Webhook 控制器能立即回應，訊息由背景工作執行緒處理。
/// </summary>
public interface IMessageQueue
{
    /// <summary>將訊息加入佇列</summary>
    ValueTask EnqueueAsync(QueuedMessage message, CancellationToken cancellationToken = default);

    /// <summary>從佇列取出訊息（阻塞至有訊息可用）</summary>
    ValueTask<QueuedMessage> DequeueAsync(CancellationToken cancellationToken = default);
}

/// <summary>佇列訊息封裝 — 攜帶統一訊息與來源平台資訊</summary>
public record QueuedMessage(
    UnifiedMessage Message,
    string Platform
);
