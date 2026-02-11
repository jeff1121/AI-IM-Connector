namespace AiImConnector.Configuration;

/// <summary>
/// Copilot SDK 連線設定
/// </summary>
public class AcpSettings
{
    public const string SectionName = "Acp";

    /// <summary>Copilot CLI 執行檔路徑（留空則使用 PATH 中的預設位置）</summary>
    public string CliPath { get; set; } = string.Empty;

    /// <summary>回應逾時時間（秒）</summary>
    public int ResponseTimeoutSeconds { get; set; } = 120;
}
