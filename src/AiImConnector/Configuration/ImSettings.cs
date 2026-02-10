namespace AiImConnector.Configuration;

/// <summary>
/// LINE 平台設定
/// </summary>
public class LineSettings
{
    public const string SectionName = "Im:Line";

    /// <summary>LINE Channel Access Token</summary>
    public string ChannelAccessToken { get; set; } = string.Empty;

    /// <summary>LINE Channel Secret（用於 Webhook 簽名驗證）</summary>
    public string ChannelSecret { get; set; } = string.Empty;
}

/// <summary>
/// Telegram 平台設定
/// </summary>
public class TelegramSettings
{
    public const string SectionName = "Im:Telegram";

    /// <summary>Telegram Bot Token</summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>Webhook Secret Token（用於驗證請求來源）</summary>
    public string SecretToken { get; set; } = string.Empty;
}
