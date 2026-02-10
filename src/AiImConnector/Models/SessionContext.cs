namespace AiImConnector.Models;

/// <summary>
/// 對話上下文 — 維持使用者與 AI 的對話歷史
/// </summary>
public class SessionContext
{
    /// <summary>Session 識別碼（格式：{平台}:{使用者ID}）</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>來源平台</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>使用者識別碼</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>對應的 ACP Session ID</summary>
    public string? AcpSessionId { get; set; }

    /// <summary>對話歷史紀錄</summary>
    public List<ConversationEntry> History { get; set; } = new();

    /// <summary>建立時間</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>最後活動時間</summary>
    public DateTimeOffset LastActiveAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>更新最後活動時間</summary>
    public void Touch() => LastActiveAt = DateTimeOffset.UtcNow;
}

/// <summary>
/// 對話紀錄項目
/// </summary>
public class ConversationEntry
{
    /// <summary>角色（user / assistant）</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>文字內容</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>附帶的多媒體描述</summary>
    public List<string> MediaDescriptions { get; set; } = new();

    /// <summary>時間戳</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
