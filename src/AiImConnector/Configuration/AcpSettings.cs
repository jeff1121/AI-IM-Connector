namespace AiImConnector.Configuration;

/// <summary>
/// ACP（Agent Client Protocol）連線設定
/// </summary>
public class AcpSettings
{
    public const string SectionName = "Acp";

    /// <summary>預設 ACP Server URL</summary>
    public string DefaultServerUrl { get; set; } = "http://localhost:8080";

    /// <summary>連線逾時時間（秒）</summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>回應串流讀取逾時時間（秒）</summary>
    public int StreamTimeoutSeconds { get; set; } = 120;

    /// <summary>重試次數</summary>
    public int RetryCount { get; set; } = 3;
}
