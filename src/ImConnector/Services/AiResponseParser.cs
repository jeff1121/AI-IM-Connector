using System.Text.RegularExpressions;
using AiImConnector.Models;

namespace AiImConnector.Services;

/// <summary>
/// AI 回應解析器 — 從 AI 文字回應中擷取多媒體內容（Markdown 圖片、圖片 URL、Base64 Data URI）
/// 並偵測本機檔案路徑參照。所有正規表達式均設有 1 秒逾時，防止 ReDoS 攻擊。
/// </summary>
public static class AiResponseParser
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    // Markdown 圖片語法：![alt](url 或 data:...) 
    private static readonly Regex MarkdownImageRegex = new(
        @"!\[([^\]]*)\]\(((?:https?://[^\s)]+|data:[^)]+))\)",
        RegexOptions.Compiled, RegexTimeout);

    // 獨立圖片 URL（不在 Markdown 語法內）
    private static readonly Regex StandaloneImageUrlRegex = new(
        @"(?<=\s|^)(https?://[^\s<>""']+\.(?:png|jpe?g|gif|webp|bmp)(?:\?[^\s<>""']*)?)(?=[\s,。、！？)）\]」]|$)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline, RegexTimeout);

    // Base64 Data URI（不在 Markdown 語法內）— 允許 base64 內含換行/空白
    private static readonly Regex Base64DataUriRegex = new(
        @"data:(image/[\w+.\-]+);base64,([A-Za-z0-9+/=\s]+)",
        RegexOptions.Compiled, RegexTimeout);

    // 本機檔案路徑（Windows 或 Unix 絕對路徑，指向圖片檔）
    private static readonly Regex LocalFilePathRegex = new(
        @"(?:`?)([A-Za-z]:\\(?:[^\s`<>""*?|:]+\\)*[^\s`<>""*?|:]+\.(?:png|jpe?g|gif|webp|bmp)" +
        @"|/(?:home|Users|tmp|var|opt)/(?:[^\s`<>""*?|]+/)*[^\s`<>""*?|]+\.(?:png|jpe?g|gif|webp|bmp))(?:`?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase, RegexTimeout);

    /// <summary>解析 AI 回應文字，擷取多媒體內容並回傳結構化結果</summary>
    public static RouterResponse Parse(string aiResponse)
    {
        if (string.IsNullOrEmpty(aiResponse))
            return new RouterResponse { Text = aiResponse ?? string.Empty };

        var mediaList = new List<MediaContent>();
        var text = aiResponse;

        // 1. 擷取 Markdown 圖片語法：![alt](url) 或 ![alt](data:...)
        text = MarkdownImageRegex.Replace(text, match =>
        {
            var altText = match.Groups[1].Value;
            var url = match.Groups[2].Value;

            var base64Match = Base64DataUriRegex.Match(url);
            if (base64Match.Success)
            {
                mediaList.Add(new MediaContent
                {
                    Type = MediaType.Image,
                    MimeType = base64Match.Groups[1].Value,
                    Base64Data = SanitizeBase64(base64Match.Groups[2].Value),
                    FileName = !string.IsNullOrEmpty(altText) ? SanitizeFileName(altText) : null
                });
                return "";
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                mediaList.Add(new MediaContent
                {
                    Type = MediaType.Image,
                    SourceUrl = url,
                    MimeType = GuessMimeType(url),
                    FileName = !string.IsNullOrEmpty(altText) ? SanitizeFileName(altText) : null
                });
                return "";
            }

            return match.Value;
        });

        // 2. 擷取獨立 Base64 Data URI（不在 Markdown 語法內）
        text = Base64DataUriRegex.Replace(text, match =>
        {
            mediaList.Add(new MediaContent
            {
                Type = MediaType.Image,
                MimeType = match.Groups[1].Value,
                Base64Data = SanitizeBase64(match.Groups[2].Value)
            });
            return "";
        });

        // 3. 擷取獨立圖片 URL（不在 Markdown 語法內）
        text = StandaloneImageUrlRegex.Replace(text, match =>
        {
            var url = match.Groups[1].Value;
            if (mediaList.All(m => m.SourceUrl != url))
            {
                mediaList.Add(new MediaContent
                {
                    Type = MediaType.Image,
                    SourceUrl = url,
                    MimeType = GuessMimeType(url)
                });
            }
            return "";
        });

        // 清理多餘空行
        text = Regex.Replace(text.Trim(), @"\n{3,}", "\n\n");

        // 4. 偵測本機檔案路徑參照
        var localPaths = new List<string>();
        foreach (Match match in LocalFilePathRegex.Matches(text))
        {
            localPaths.Add(match.Groups[1].Value);
        }

        return new RouterResponse
        {
            Text = text,
            MediaContents = mediaList,
            LocalFilePaths = localPaths
        };
    }

    private static string GuessMimeType(string url)
    {
        var path = Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.AbsolutePath.ToLowerInvariant()
            : url.ToLowerInvariant();

        return path switch
        {
            var p when p.EndsWith(".png") => "image/png",
            var p when p.EndsWith(".jpg") || p.EndsWith(".jpeg") => "image/jpeg",
            var p when p.EndsWith(".gif") => "image/gif",
            var p when p.EndsWith(".webp") => "image/webp",
            var p when p.EndsWith(".bmp") => "image/bmp",
            _ => "image/png"
        };
    }

    private static readonly Regex WhitespaceRegex = new(@"\s", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>移除 Base64 字串中的空白與換行字元</summary>
    private static string SanitizeBase64(string base64) => WhitespaceRegex.Replace(base64, "");

    /// <summary>清理檔名中的非法字元</summary>
    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Where(c => !invalid.Contains(c)));
    }
}
