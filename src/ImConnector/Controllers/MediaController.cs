using AiImConnector.Services.Media;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AiImConnector.Controllers;

/// <summary>
/// 多媒體暫存 API — 提供暫存多媒體檔案的 HTTP 存取端點，
/// 供 IM 平台（如 LINE）透過公開 URL 取得多媒體內容。
/// </summary>
[ApiController]
[Route("api/media")]
[EnableRateLimiting("media")]
public class MediaController : ControllerBase
{
    private readonly MediaHostingService _hostingService;

    public MediaController(MediaHostingService hostingService)
    {
        _hostingService = hostingService;
    }

    /// <summary>取得暫存的多媒體檔案</summary>
    [HttpGet("{id}")]
    public IActionResult Get(string id)
    {
        // 驗證 id 格式（32 字元 hex GUID）
        if (string.IsNullOrEmpty(id) || id.Length != 32 || !id.All(char.IsAsciiHexDigit))
            return NotFound();

        var media = _hostingService.GetMedia(id);
        if (media == null) return NotFound();

        return File(media.Data, media.MimeType, media.FileName);
    }
}
