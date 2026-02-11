namespace AiImConnector.Configuration;

/// <summary>
/// ACP Server 連線設定
/// </summary>
public class AcpSettings
{
    public const string SectionName = "Acp";

    /// <summary>ACP Server 主機位址</summary>
    public string ServerHost { get; set; } = "localhost";

    /// <summary>ACP Server 連接埠</summary>
    public int ServerPort { get; set; } = 10080;

    /// <summary>回應逾時時間（秒）</summary>
    public int ResponseTimeoutSeconds { get; set; } = 120;
}
