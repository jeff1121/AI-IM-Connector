using AiImConnector.Services.Media;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AiImConnector.HealthChecks;

/// <summary>
/// 多媒體暫存服務健康檢查 — 監控暫存使用量
/// </summary>
public class MediaHostingHealthCheck : IHealthCheck
{
    private readonly MediaHostingService _mediaHostingService;

    public MediaHostingHealthCheck(MediaHostingService mediaHostingService)
    {
        _mediaHostingService = mediaHostingService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stats = _mediaHostingService.GetStats();
        var data = new Dictionary<string, object>
        {
            { "entries", stats.EntryCount },
            { "totalBytes", stats.TotalBytes },
            { "maxEntries", stats.MaxEntries },
            { "maxTotalBytes", stats.MaxTotalBytes }
        };

        // 使用量超過 80% 時警告
        var usageRatio = Math.Max(
            (double)stats.EntryCount / stats.MaxEntries,
            (double)stats.TotalBytes / stats.MaxTotalBytes);

        if (usageRatio > 0.8)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"多媒體暫存使用量偏高（{usageRatio:P0}）", data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"多媒體暫存正常（{stats.EntryCount} 筆 / {stats.TotalBytes / 1024 / 1024} MB）", data: data));
    }
}
