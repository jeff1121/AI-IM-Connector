using System.Threading.Channels;

namespace AiImConnector.Services.Queue;

/// <summary>
/// 記憶體內訊息佇列 — 使用 Channel&lt;T&gt; 實作高效能非同步生產者-消費者模式。
/// 設有上限（1000 則），佇列滿時丟棄最舊的訊息並記錄警告。
/// </summary>
public class InMemoryMessageQueue : IMessageQueue
{
    private readonly Channel<QueuedMessage> _channel;

    public InMemoryMessageQueue()
    {
        _channel = Channel.CreateBounded<QueuedMessage>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public ValueTask EnqueueAsync(QueuedMessage message, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(message, cancellationToken);

    public ValueTask<QueuedMessage> DequeueAsync(CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAsync(cancellationToken);
}
