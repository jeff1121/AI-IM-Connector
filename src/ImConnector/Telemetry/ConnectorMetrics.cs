using System.Diagnostics.Metrics;

namespace AiImConnector.Telemetry;

/// <summary>
/// 應用程式自訂度量 — 追蹤訊息處理、Session 與多媒體暫存指標
/// </summary>
public class ConnectorMetrics
{
    public const string MeterName = "AiImConnector";

    private readonly Counter<long> _messagesReceived;
    private readonly Counter<long> _messagesRouted;
    private readonly Counter<long> _messagesFailed;
    private readonly Counter<long> _commandsProcessed;
    private readonly Histogram<double> _routingDuration;
    private readonly Counter<long> _sessionsCreated;
    private readonly Counter<long> _sessionsCleared;
    private readonly Counter<long> _sessionsRebuilt;
    private readonly Counter<long> _mediaHosted;
    private readonly Counter<long> _mediaExpired;

    public ConnectorMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _messagesReceived = meter.CreateCounter<long>(
            "connector.messages.received", "messages", "收到的 Webhook 訊息數");

        _messagesRouted = meter.CreateCounter<long>(
            "connector.messages.routed", "messages", "成功路由的訊息數");

        _messagesFailed = meter.CreateCounter<long>(
            "connector.messages.failed", "messages", "路由失敗的訊息數");

        _commandsProcessed = meter.CreateCounter<long>(
            "connector.commands.processed", "commands", "處理的使用者指令數");

        _routingDuration = meter.CreateHistogram<double>(
            "connector.routing.duration", "ms", "訊息路由耗時（毫秒）");

        _sessionsCreated = meter.CreateCounter<long>(
            "connector.sessions.created", "sessions", "建立的 Session 數");

        _sessionsCleared = meter.CreateCounter<long>(
            "connector.sessions.cleared", "sessions", "清除的 Session 數");

        _sessionsRebuilt = meter.CreateCounter<long>(
            "connector.sessions.rebuilt", "sessions", "重建的 Session 數");

        _mediaHosted = meter.CreateCounter<long>(
            "connector.media.hosted", "items", "暫存的多媒體數");

        _mediaExpired = meter.CreateCounter<long>(
            "connector.media.expired", "items", "過期清除的多媒體數");
    }

    public void RecordMessageReceived(string platform)
        => _messagesReceived.Add(1, new KeyValuePair<string, object?>("platform", platform));

    public void RecordMessageRouted(string platform)
        => _messagesRouted.Add(1, new KeyValuePair<string, object?>("platform", platform));

    public void RecordMessageFailed(string platform)
        => _messagesFailed.Add(1, new KeyValuePair<string, object?>("platform", platform));

    public void RecordCommandProcessed(string platform, string command)
        => _commandsProcessed.Add(1,
            new KeyValuePair<string, object?>("platform", platform),
            new KeyValuePair<string, object?>("command", command));

    public void RecordRoutingDuration(string platform, double durationMs)
        => _routingDuration.Record(durationMs, new KeyValuePair<string, object?>("platform", platform));

    public void RecordSessionCreated(string platform)
        => _sessionsCreated.Add(1, new KeyValuePair<string, object?>("platform", platform));

    public void RecordSessionCleared(string platform)
        => _sessionsCleared.Add(1, new KeyValuePair<string, object?>("platform", platform));

    public void RecordSessionRebuilt(string platform)
        => _sessionsRebuilt.Add(1, new KeyValuePair<string, object?>("platform", platform));

    public void RecordMediaHosted()
        => _mediaHosted.Add(1);

    public void RecordMediaExpired(int count)
        => _mediaExpired.Add(count);
}
