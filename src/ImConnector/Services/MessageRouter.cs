using AiImConnector.Models;
using AiImConnector.Services.Acp;
using AiImConnector.Services.Media;
using Microsoft.Extensions.Logging;

namespace AiImConnector.Services;

/// <summary>
/// 訊息路由服務 — 負責將 IM 訊息轉發到 Copilot SDK，並將回應轉回 IM。
/// 處理使用者指令（/clear、/help、/status）及多媒體內容描述的 Prompt 組合。
/// 支援解析 AI 回應中的多媒體內容（圖片 URL、Base64），暫存為臨時 URL 附加於文字回應中。
/// </summary>
public class MessageRouter
{
    private readonly CopilotSessionManager _sessionManager;
    private readonly IMediaHandler _mediaHandler;
    private readonly MediaHostingService _mediaHostingService;
    private readonly ILogger<MessageRouter> _logger;

    public MessageRouter(
        CopilotSessionManager sessionManager,
        IMediaHandler mediaHandler,
        MediaHostingService mediaHostingService,
        ILogger<MessageRouter> logger)
    {
        _sessionManager = sessionManager;
        _mediaHandler = mediaHandler;
        _mediaHostingService = mediaHostingService;
        _logger = logger;
    }

    /// <summary>處理傳入的訊息並取得 AI 回應（含多媒體解析）</summary>
    public async Task<RouterResponse> RouteMessageAsync(UnifiedMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("收到訊息 — 平台：{Platform}，使用者：{UserId}", message.Platform, message.UserId);

        // 處理指令
        if (message.IsCommand)
        {
            var commandResult = await HandleCommandAsync(message, cancellationToken);
            return new RouterResponse { Text = commandResult };
        }

        // 組合 Prompt（文字 + 多媒體描述）
        var prompt = BuildPrompt(message);

        // 發送到 Copilot 並取得回應
        try
        {
            var aiText = await _sessionManager.SendMessageAsync(
                message.Platform,
                message.UserId,
                prompt,
                cancellationToken);

            // 解析 AI 回應中的多媒體內容
            var response = AiResponseParser.Parse(aiText);

            // 若 AI 回應僅有本機路徑而無嵌入媒體，自動要求 AI 重新提供
            if (response.HasLocalFilePaths && !response.HasMedia)
            {
                _logger.LogWarning("AI 回應包含本機路徑但未嵌入多媒體，發送修正指令：{Paths}",
                    string.Join(", ", response.LocalFilePaths));

                var retryPrompt =
                    "你剛才將檔案存在本機路徑，但我無法存取本機檔案系統。" +
                    "請將該檔案的內容直接嵌入回覆中，使用 Markdown 圖片語法搭配 base64 data URI 格式：" +
                    "![描述](data:image/png;base64,...)。" +
                    "請直接提供圖片資料，不要再存成檔案。";

                var retryText = await _sessionManager.SendMessageAsync(
                    message.Platform,
                    message.UserId,
                    retryPrompt,
                    cancellationToken);

                response = AiResponseParser.Parse(retryText);

                if (response.HasLocalFilePaths && !response.HasMedia)
                {
                    _logger.LogWarning("重試後仍未取得嵌入圖片");
                }
            }

            // 將 Base64 多媒體暫存並產生公開 URL
            foreach (var media in response.MediaContents)
            {
                if (!string.IsNullOrEmpty(media.Base64Data) && string.IsNullOrEmpty(media.SourceUrl))
                {
                    var hostedUrl = _mediaHostingService.HostMedia(media);
                    if (hostedUrl != null)
                    {
                        media.SourceUrl = hostedUrl;
                    }
                }
            }

            // 將多媒體 URL 附加到文字回應中，使用者點擊連結即可取得圖片
            // 不依賴 IM 平台的多媒體推送 API，相容性更高
            var hostedMediaLinks = response.MediaContents
                .Where(m => !string.IsNullOrEmpty(m.SourceUrl))
                .ToList();

            if (hostedMediaLinks.Count > 0)
            {
                _logger.LogInformation("AI 回應包含 {Count} 個多媒體內容，將以臨時 URL 附加於文字回應", hostedMediaLinks.Count);

                var linkLines = hostedMediaLinks
                    .Select(m => $"🖼️ {m.FileName ?? "圖片"}：{m.SourceUrl}");

                response.Text = string.IsNullOrEmpty(response.Text)
                    ? string.Join("\n", linkLines)
                    : response.Text + "\n\n" + string.Join("\n", linkLines);
            }
            else if (response.MediaContents.Count > 0)
            {
                _logger.LogWarning("AI 回應包含 {Count} 個多媒體但暫存失敗（PublicBaseUrl 可能未設定），無法產生下載連結",
                    response.MediaContents.Count);
            }

            // 清除 MediaContents — 已轉為文字 URL 或暫存失敗均不再交由 Controller 推送
            response.MediaContents.Clear();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "訊息路由失敗 — 平台：{Platform}，使用者：{UserId}", message.Platform, message.UserId);
            return new RouterResponse { Text = "⚠️ AI 回應發生錯誤，請稍後再試。" };
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
