namespace AiImConnector.Models;

/// <summary>
/// 訊息方向
/// </summary>
public enum MessageDirection
{
    /// <summary>使用者傳入</summary>
    Incoming,
    /// <summary>回覆使用者</summary>
    Outgoing
}

/// <summary>
/// 統一訊息格式 — 跨平台訊息抽象層
/// </summary>
public class UnifiedMessage
{
    /// <summary>訊息唯一識別碼</summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>來源平台（Line、Telegram 等）</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>使用者識別碼（平台內的使用者 ID）</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>聊天室/群組識別碼</summary>
    public string? ChatId { get; set; }

    /// <summary>訊息方向</summary>
    public MessageDirection Direction { get; set; }

    /// <summary>文字內容</summary>
    public string? Text { get; set; }

    /// <summary>多媒體附件清單</summary>
    public List<MediaContent> MediaContents { get; set; } = new();

    /// <summary>是否為指令（例如 /clear、/help）</summary>
    public bool IsCommand => Text?.StartsWith("/") == true;

    /// <summary>指令名稱（不含 /）</summary>
    public string? CommandName => IsCommand ? Text?.Split(' ').FirstOrDefault()?.TrimStart('/') : null;

    /// <summary>指令參數</summary>
    public string? CommandArgs => IsCommand ? string.Join(' ', Text?.Split(' ').Skip(1) ?? Enumerable.Empty<string>()) : null;

    /// <summary>訊息時間戳</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>平台原始回覆 Token（LINE 使用）</summary>
    public string? ReplyToken { get; set; }
}
