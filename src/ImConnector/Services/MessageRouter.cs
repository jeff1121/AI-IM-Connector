using AiImConnector.Models;
using AiImConnector.Services.Acp;
using AiImConnector.Services.Media;
using Microsoft.Extensions.Logging;

namespace AiImConnector.Services;

/// <summary>
/// 訊息路由服務 — 負責將 IM 訊息轉發到 Copilot SDK，並將回應轉回 IM。
/// 處理使用者指令（/clear、/help、/status）及多媒體內容描述的 Prompt 組合。
/// </summary>
public class MessageRouter
{
    private readonly CopilotSessionManager _sessionManager;
    private readonly IMediaHandler _mediaHandler;
    private readonly ILogger<MessageRouter> _logger;

    public MessageRouter(
        CopilotSessionManager sessionManager,
        IMediaHandler mediaHandler,
        ILogger<MessageRouter> logger)
    {
        _sessionManager = sessionManager;
        _mediaHandler = mediaHandler;
        _logger = logger;
    }

    /// <summary>處理傳入的訊息並取得 AI 回應</summary>
    public async Task<string> RouteMessageAsync(UnifiedMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("收到訊息 — 平台：{Platform}，使用者：{UserId}", message.Platform, message.UserId);

        // 處理指令
        if (message.IsCommand)
        {
            return await HandleCommandAsync(message, cancellationToken);
        }

        // 組合 Prompt（文字 + 多媒體描述）
        var prompt = BuildPrompt(message);

        // 發送到 Copilot 並取得回應
        try
        {
            var response = await _sessionManager.SendMessageAsync(
                message.Platform,
                message.UserId,
                prompt,
                cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "訊息路由失敗 — 平台：{Platform}，使用者：{UserId}", message.Platform, message.UserId);
            return "⚠️ AI 回應發生錯誤，請稍後再試。";
        }
    }

    /// <summary>組合 Prompt 內容</summary>
    private string BuildPrompt(UnifiedMessage message)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(message.Text))
        {
            parts.Add(message.Text);
        }

        // 將多媒體內容描述加入 Prompt
        foreach (var media in message.MediaContents)
        {
            var description = media.Type switch
            {
                MediaType.Image => $"[使用者傳送了一張圖片：{media.FileName ?? media.MimeType}]",
                MediaType.Video => $"[使用者傳送了一段影片：{media.FileName ?? media.MimeType}]",
                MediaType.Audio => $"[使用者傳送了一段音訊：{media.FileName ?? media.MimeType}]",
                MediaType.File => $"[使用者傳送了一個檔案：{media.FileName ?? media.MimeType}]",
                _ => $"[使用者傳送了多媒體內容]"
            };
            parts.Add(description);
        }

        return parts.Count > 0 ? string.Join("\n", parts) : "（空訊息）";
    }

    /// <summary>處理使用者指令</summary>
    private async Task<string> HandleCommandAsync(UnifiedMessage message, CancellationToken cancellationToken)
    {
        switch (message.CommandName?.ToLowerInvariant())
        {
            case "clear":
                await _sessionManager.ClearSessionAsync(message.Platform, message.UserId, cancellationToken);
                return "🗑️ 對話歷史已清除。";

            case "help":
                return "📋 可用指令：\n" +
                       "/clear — 清除對話歷史\n" +
                       "/help — 顯示此說明\n" +
                       "/status — 查看目前連線狀態";

            case "status":
                var binding = _sessionManager.GetBinding(message.Platform);
                var sessionId = CopilotSessionManager.BuildSessionId(message.Platform, message.UserId);
                return $"📊 連線狀態：\n" +
                       $"平台：{message.Platform}\n" +
                       $"Agent：{binding?.AgentName ?? "未配置"}\n" +
                       $"模型：{binding?.Model ?? "未配置"}\n" +
                       $"Session：{sessionId}";

            default:
                return $"❓ 未知指令：/{message.CommandName}。輸入 /help 查看可用指令。";
        }
    }
}
