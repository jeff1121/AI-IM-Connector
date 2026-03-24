using AiImConnector.Adapters;
using Microsoft.Extensions.Logging;

namespace AiImConnector.Services.Queue;

/// <summary>
/// 訊息佇列處理器 — 背景服務，持續從佇列取出訊息並處理。
/// 訊息路由至 AI 後，透過對應的 IImAdapter 回覆給使用者。
/// 支援多平台自動分派（依據訊息的 Platform 欄位選擇對應適配器）。
/// </summary>
public class MessageQueueWorker : BackgroundService
{
    private readonly IMessageQueue _queue;
    private readonly MessageRouter _router;
    private readonly IEnumerable<IImAdapter> _adapters;
    private readonly ILogger<MessageQueueWorker> _logger;

    public MessageQueueWorker(
        IMessageQueue queue,
        MessageRouter router,
        IEnumerable<IImAdapter> adapters,
        ILogger<MessageQueueWorker> logger)
    {
        _queue = queue;
        _router = router;
        _adapters = adapters;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("訊息佇列處理器已啟動");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queued = await _queue.DequeueAsync(stoppingToken);
                await ProcessMessageAsync(queued, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "訊息佇列處理器發生未預期錯誤");
                // 短暫延遲避免錯誤時的緊密迴圈
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        _logger.LogInformation("訊息佇列處理器已停止");
    }

    private async Task ProcessMessageAsync(QueuedMessage queued, CancellationToken cancellationToken)
    {
        var adapter = _adapters.FirstOrDefault(a =>
            a.PlatformName.Equals(queued.Platform, StringComparison.OrdinalIgnoreCase));

        if (adapter == null)
        {
            _logger.LogWarning("找不到平台 {Platform} 的適配器，訊息已丟棄", queued.Platform);
            return;
        }

        try
        {
            var response = await _router.RouteMessageAsync(queued.Message);

            // 發送文字回應
            if (!string.IsNullOrEmpty(response.Text))
            {
                await adapter.ReplyTextAsync(queued.Message, response.Text, cancellationToken);
                // LINE Reply Token 已消耗，後續多媒體改用 Push API
                queued.Message.ReplyToken = null;
            }

            // 發送多媒體回應
            foreach (var media in response.MediaContents)
            {
                await adapter.ReplyMediaAsync(queued.Message, media, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "處理 {Platform} 訊息失敗：{UserId}",
                queued.Platform, queued.Message.UserId);
        }
    }
}
