using AiImConnector.Models;

namespace AiImConnector.Services.Media;

/// <summary>
/// 多媒體處理介面
/// </summary>
public interface IMediaHandler
{
    /// <summary>從 URL 下載多媒體內容並轉換為 Base64</summary>
    /// <param name="url">多媒體檔案 URL</param>
    /// <param name="headers">額外的 HTTP 標頭（例如授權）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>含 Base64 資料的 MediaContent</returns>
    Task<MediaContent> DownloadAsBase64Async(string url, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>從多媒體內容建立 ACP 附件</summary>
    Task<AcpAttachment> ToAcpAttachmentAsync(MediaContent media, CancellationToken cancellationToken = default);

    /// <summary>從 MIME 類型判斷多媒體類型</summary>
    MediaType DetectMediaType(string mimeType);
}
