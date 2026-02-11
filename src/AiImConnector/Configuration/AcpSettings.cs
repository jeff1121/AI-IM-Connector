namespace AiImConnector.Configuration;

/// <summary>
/// ACP (Copilot SDK) 連線設定
/// </summary>
public class AcpSettings
{
    public const string SectionName = "Acp";

    /// <summary>外部 ACP Server URL（例如 "localhost:10080"），留空則由 SDK 自動啟動 CLI</summary>
    public string? CliUrl { get; set; } = "localhost:10080";

    /// <summary>Copilot CLI 路徑（僅在未設定 CliUrl 時使用）</summary>
    public string? CliPath { get; set; }

    /// <summary>回應逾時時間（秒）</summary>
    public int ResponseTimeoutSeconds { get; set; } = 120;
}
