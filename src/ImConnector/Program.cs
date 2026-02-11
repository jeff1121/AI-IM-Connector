using AiImConnector.Adapters.Line;
using AiImConnector.Adapters.Telegram;
using AiImConnector.Configuration;
using AiImConnector.Middleware;
using AiImConnector.Services;
using AiImConnector.Services.Acp;
using AiImConnector.Services.Media;

var builder = WebApplication.CreateBuilder(args);

// === 設定綁定 ===
builder.Services.Configure<LineSettings>(builder.Configuration.GetSection(LineSettings.SectionName));
builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection(TelegramSettings.SectionName));
builder.Services.Configure<AcpSettings>(builder.Configuration.GetSection(AcpSettings.SectionName));
builder.Services.Configure<AgentBindingSettings>(builder.Configuration.GetSection(AgentBindingSettings.SectionName));

// === Copilot SDK 服務註冊（Singleton 確保 Session 全域共享） ===
builder.Services.AddSingleton<ICopilotClientService, CopilotClientService>();
builder.Services.AddSingleton<CopilotSessionManager>();
builder.Services.AddHttpClient<IMediaHandler, MediaHandler>();
builder.Services.AddSingleton<MessageRouter>();

// === IM 適配器註冊 ===
builder.Services.AddHttpClient<LineAdapter>();
builder.Services.AddSingleton<TelegramAdapter>();

// === ASP.NET Core 服務 ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// === Copilot CLI 生命週期管理 ===
builder.Services.AddHostedService<CopilotLifecycleService>();

var app = builder.Build();

// === 中介層管線 ===
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<WebhookValidationMiddleware>();

// 靜態檔案（服務條款等頁面）
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// 健康檢查端點
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

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
