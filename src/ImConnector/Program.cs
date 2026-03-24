using AiImConnector.Adapters.Line;
using AiImConnector.Adapters.Telegram;
using AiImConnector.Configuration;
using AiImConnector.HealthChecks;
using AiImConnector.Middleware;
using AiImConnector.Services;
using AiImConnector.Services.Acp;
using AiImConnector.Services.Media;
using AiImConnector.Telemetry;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// === 設定綁定 ===
builder.Services.Configure<ConnectorSettings>(builder.Configuration.GetSection(ConnectorSettings.SectionName));
builder.Services.Configure<LineSettings>(builder.Configuration.GetSection(LineSettings.SectionName));
builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection(TelegramSettings.SectionName));
builder.Services.Configure<AcpSettings>(builder.Configuration.GetSection(AcpSettings.SectionName));
builder.Services.Configure<AgentBindingSettings>(builder.Configuration.GetSection(AgentBindingSettings.SectionName));

// === Copilot SDK 服務註冊（Singleton 確保 Session 全域共享） ===
builder.Services.AddSingleton<ICopilotClientService, CopilotClientService>();
builder.Services.AddSingleton<CopilotSessionManager>();
builder.Services.AddHttpClient<IMediaHandler, MediaHandler>();
builder.Services.AddSingleton<MediaHostingService>();
builder.Services.AddSingleton<MessageRouter>();

// === IM 適配器註冊 ===
builder.Services.AddHttpClient<LineAdapter>();
builder.Services.AddSingleton<TelegramAdapter>();

// === ASP.NET Core 服務 ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// === 健康檢查 ===
builder.Services
    .AddHealthChecks()
    .AddCheck<CopilotHealthCheck>("copilot-cli", tags: new[] { "ready" })
    .AddCheck<MediaHostingHealthCheck>("media-hosting", tags: new[] { "ready" });

// === OpenTelemetry 監控 ===
builder.Services.AddSingleton<ConnectorMetrics>();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("AiImConnector"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter(ConnectorMetrics.MeterName)
        .AddPrometheusExporter());

// === Rate Limiting（按 IP + 路徑分區限流） ===
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Webhook 端點 — 每個 IP 每分鐘最多 60 次請求
    options.AddPolicy("webhook", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0
            }));

    // 多媒體下載端點 — 每個 IP 每分鐘最多 120 次
    options.AddPolicy("media", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0
            }));

    // 全域 fallback — 每個 IP 每分鐘最多 200 次
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueLimit = 0
            }));

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }
        await Task.CompletedTask;
    };
});

// === Copilot CLI 生命週期管理 ===
builder.Services.AddHostedService<CopilotLifecycleService>();

var app = builder.Build();

// === 中介層管線 ===
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<WebhookValidationMiddleware>();
app.UseRateLimiter();

// 靜態檔案（服務條款等頁面）
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// 健康檢查端點（結構化 JSON 回應）
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Prometheus 指標端點
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();

/// <summary>
/// Copilot CLI 生命週期管理背景服務 — 隨應用程式啟動/停止 Copilot CLI
/// </summary>
public class CopilotLifecycleService : BackgroundService
{
    private readonly ICopilotClientService _clientService;
    private readonly ILogger<CopilotLifecycleService> _logger;

    public CopilotLifecycleService(ICopilotClientService clientService, ILogger<CopilotLifecycleService> logger)
    {
        _clientService = clientService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _clientService.StartAsync(stoppingToken);
            _logger.LogInformation("Copilot CLI 背景服務已啟動");

            // 等待應用程式停止信號
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // 正常停止
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copilot CLI 背景服務發生錯誤");
        }
        finally
        {
            await _clientService.StopAsync();
            _logger.LogInformation("Copilot CLI 背景服務已停止");
        }
    }
}

// 提供給整合測試使用
public partial class Program { }
