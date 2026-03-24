using AiImConnector.Services.Acp;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AiImConnector.HealthChecks;

/// <summary>
/// Copilot CLI 連線狀態健康檢查
/// </summary>
public class CopilotHealthCheck : IHealthCheck
{
    private readonly ICopilotClientService _clientService;

    public CopilotHealthCheck(ICopilotClientService clientService)
    {
        _clientService = clientService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_clientService.IsConnected)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Copilot CLI 已連線"));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy("Copilot CLI 未連線"));
    }
}
