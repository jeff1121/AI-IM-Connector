using AiImConnector.Models;

namespace AiImConnector.Adapters;

/// <summary>
/// IM 適配器介面 — 定義各 IM 平台共通的操作
/// </summary>
public interface IImAdapter
{
    /// <summary>平台名稱（例如 Line、Telegram）</summary>
    string PlatformName { get; }

    /// <summary>回覆文字訊息給使用者</summary>
    /// <param name="message">統一訊息格式（含目標使用者資訊）</param>
    /// <param name="replyText">回覆文字內容</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ReplyTextAsync(UnifiedMessage message, string replyText, CancellationToken cancellationToken = default);

    /// <summary>回覆多媒體訊息給使用者</summary>
    /// <param name="message">統一訊息格式</param>
    /// <param name="media">多媒體內容</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ReplyMediaAsync(UnifiedMessage message, MediaContent media, CancellationToken cancellationToken = default);
}
