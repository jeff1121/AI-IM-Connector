namespace AiImConnector.Models;

/// <summary>
/// 多媒體類型列舉
/// </summary>
public enum MediaType
{
    /// <summary>圖片</summary>
    Image,
    /// <summary>影片</summary>
    Video,
    /// <summary>音訊</summary>
    Audio,
    /// <summary>檔案</summary>
    File
}

/// <summary>
/// 多媒體內容模型 — 封裝各平台的多媒體資料
/// </summary>
public class MediaContent
{
    /// <summary>多媒體類型</summary>
    public MediaType Type { get; set; }

    /// <summary>原始 URL（來自 IM 平台）</summary>
    public string? SourceUrl { get; set; }

    /// <summary>Base64 編碼的內容（用於傳送給 ACP Agent）</summary>
    public string? Base64Data { get; set; }

    /// <summary>MIME 類型（例如 image/png、video/mp4）</summary>
    public string MimeType { get; set; } = "application/octet-stream";

    /// <summary>檔案名稱</summary>
    public string? FileName { get; set; }

    /// <summary>檔案大小（bytes）</summary>
    public long? FileSize { get; set; }

    /// <summary>預覽圖 URL（影片用）</summary>
    public string? ThumbnailUrl { get; set; }
}
