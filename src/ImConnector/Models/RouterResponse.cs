namespace AiImConnector.Models;

/// <summary>
/// AI 回應結果 — 包含文字、多媒體內容及本機檔案路徑偵測
/// </summary>
public class RouterResponse
{
    /// <summary>純文字回應內容（已移除多媒體參照）</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>從 AI 回應中解析出的多媒體內容</summary>
    public List<MediaContent> MediaContents { get; set; } = new();

    /// <summary>偵測到的本機檔案路徑（AI 將檔案存在本機但未嵌入回應）</summary>
    public List<string> LocalFilePaths { get; set; } = new();

    /// <summary>是否包含多媒體內容</summary>
    public bool HasMedia => MediaContents.Count > 0;

    /// <summary>是否偵測到本機檔案路徑（表示 AI 未將檔案嵌入回應）</summary>
    public bool HasLocalFilePaths => LocalFilePaths.Count > 0;
}
