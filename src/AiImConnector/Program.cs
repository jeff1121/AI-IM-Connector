using AiImConnector.Adapters.Line;
using AiImConnector.Adapters.Telegram;
using AiImConnector.Configuration;
using AiImConnector.Middleware;
using AiImConnector.Services;
using AiImConnector.Services.Acp;
using AiImConnector.Services.Media;
using AiImConnector.Services.Session;

var builder = WebApplication.CreateBuilder(args);

// === 設定綁定 ===
builder.Services.Configure<LineSettings>(builder.Configuration.GetSection(LineSettings.SectionName));
builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection(TelegramSettings.SectionName));
builder.Services.Configure<AcpSettings>(builder.Configuration.GetSection(AcpSettings.SectionName));
builder.Services.Configure<AgentBindingSettings>(builder.Configuration.GetSection(AgentBindingSettings.SectionName));

// === 核心服務註冊 ===
builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
builder.Services.AddSingleton<SessionService>();
builder.Services.AddHttpClient<IAcpClient, AcpClient>();
builder.Services.AddSingleton<AcpSessionManager>();
builder.Services.AddHttpClient<IMediaHandler, MediaHandler>();
builder.Services.AddSingleton<MessageRouter>();

// === IM 適配器註冊 ===
builder.Services.AddHttpClient<LineAdapter>();
builder.Services.AddSingleton<TelegramAdapter>();

// === ASP.NET Core 服務 ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// === Session 清理背景服務 ===
builder.Services.AddHostedService<SessionCleanupService>();

var app = builder.Build();

// === 中介層管線 ===
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<WebhookValidationMiddleware>();

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
/// Session 定期清理背景服務
/// </summary>
public class SessionCleanupService : BackgroundService
{
    private readonly SessionService _sessionService;
    private readonly ILogger<SessionCleanupService> _logger;

    public SessionCleanupService(SessionService sessionService, ILogger<SessionCleanupService> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            try
            {
                await _sessionService.CleanupExpiredAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session 清理失敗");
            }
        }
    }
}

// 提供給整合測試使用
public partial class Program { }
