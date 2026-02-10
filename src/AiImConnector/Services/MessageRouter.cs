using AiImConnector.Models;
using AiImConnector.Services.Acp;
using AiImConnector.Services.Media;
using AiImConnector.Services.Session;
using Microsoft.Extensions.Logging;

namespace AiImConnector.Services;

/// <summary>
/// 訊息路由服務 — 負責將 IM 訊息轉發到對應的 ACP Agent，並將回應轉回 IM
/// </summary>
public class MessageRouter
{
    private readonly AcpSessionManager _acpSessionManager;
    private readonly SessionService _sessionService;
    private readonly IMediaHandler _mediaHandler;
    private readonly ILogger<MessageRouter> _logger;

    public MessageRouter(
        AcpSessionManager acpSessionManager,
        SessionService sessionService,
        IMediaHandler mediaHandler,
        ILogger<MessageRouter> logger)
    {
        _acpSessionManager = acpSessionManager;
        _sessionService = sessionService;
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

        // 取得或建立 Session
        var session = await _sessionService.GetOrCreateAsync(message.Platform, message.UserId);

        // 確保 ACP Session 已建立
        if (string.IsNullOrEmpty(session.AcpSessionId))
        {
            var binding = _acpSessionManager.GetBinding(message.Platform);
            if (binding == null)
            {
                _logger.LogError("找不到平台 '{Platform}' 的 Agent 綁定設定", message.Platform);
                return "⚠️ 系統尚未配置此平台的 AI Agent，請聯繫管理員。";
            }

            session.AcpSessionId = await _acpSessionManager.CreateSessionAsync(binding.AcpServerUrl, cancellationToken);
            await _sessionService.AddEntryAsync(message.Platform, message.UserId, "system", "Session 已建立");
        }

        // 處理多媒體附件
        var attachments = new List<AcpAttachment>();
        foreach (var media in message.MediaContents)
        {
            if (!string.IsNullOrEmpty(media.SourceUrl) && string.IsNullOrEmpty(media.Base64Data))
            {
                var downloaded = await _mediaHandler.DownloadAsBase64Async(media.SourceUrl, cancellationToken: cancellationToken);
                media.Base64Data = downloaded.Base64Data;
                media.MimeType = downloaded.MimeType;
            }
            var attachment = await _mediaHandler.ToAcpAttachmentAsync(media, cancellationToken);
            attachments.Add(attachment);
        }

        // 建立對話上下文
        var context = session.History
            .Select(h => new AcpContextMessage { Role = h.Role, Content = h.Content })
            .ToList();

        // 建立 ACP Prompt 參數
        var promptParams = new AcpPromptParams
        {
            SessionId = session.AcpSessionId!,
            Prompt = message.Text ?? "（使用者發送了多媒體內容）",
            Context = context.Count > 0 ? context : null,
            Attachments = attachments.Count > 0 ? attachments : null
        };

        // 發送到 ACP Agent
        try
        {
            var response = await _acpSessionManager.SendMessageAsync(message.Platform, promptParams, cancellationToken);

            // 紀錄對話歷史
            var mediaDescriptions = message.MediaContents
                .Select(m => $"[{m.Type}] {m.FileName ?? m.MimeType}")
                .ToList();
            await _sessionService.AddEntryAsync(message.Platform, message.UserId, "user", message.Text ?? "", mediaDescriptions);
            await _sessionService.AddEntryAsync(message.Platform, message.UserId, "assistant", response);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "訊息路由失敗 — 平台：{Platform}，使用者：{UserId}", message.Platform, message.UserId);
            return "⚠️ AI 回應發生錯誤，請稍後再試。";
        }
    }

    /// <summary>處理使用者指令</summary>
    private async Task<string> HandleCommandAsync(UnifiedMessage message, CancellationToken cancellationToken)
    {
        switch (message.CommandName?.ToLowerInvariant())
        {
            case "clear":
                await _sessionService.ClearAsync(message.Platform, message.UserId);
                return "🗑️ 對話歷史已清除。";

            case "help":
                return "📋 可用指令：\n" +
                       "/clear — 清除對話歷史\n" +
                       "/help — 顯示此說明\n" +
                       "/status — 查看目前連線狀態";

            case "status":
                var session = await _sessionService.GetOrCreateAsync(message.Platform, message.UserId);
                var binding = _acpSessionManager.GetBinding(message.Platform);
                return $"📊 連線狀態：\n" +
                       $"平台：{message.Platform}\n" +
                       $"Agent：{binding?.AgentName ?? "未配置"}\n" +
                       $"ACP Server：{binding?.AcpServerUrl ?? "未配置"}\n" +
                       $"Session：{session.AcpSessionId ?? "尚未建立"}\n" +
                       $"對話筆數：{session.History.Count}";

            default:
                return $"❓ 未知指令：/{message.CommandName}。輸入 /help 查看可用指令。";
        }
    }
}
