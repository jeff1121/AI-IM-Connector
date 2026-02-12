namespace AiImConnector.Configuration;

/// <summary>
/// Agent 綁定設定 — 定義每個 IM 平台對應的 AI 模型
/// </summary>
public class AgentBindingSettings
{
    public const string SectionName = "AgentBindings";

    /// <summary>各 IM 平台的 Agent 綁定設定</summary>
    public Dictionary<string, AgentBinding> Bindings { get; set; } = new();
}

/// <summary>
/// 單一 IM 平台的 Agent 綁定
/// </summary>
public class AgentBinding
{
    /// <summary>Agent 名稱（例如 copilot-cli、codex-cli、gemini-cli）</summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>AI 模型名稱（例如 gpt-5、claude-sonnet-4.5）</summary>
    public string Model { get; set; } = "gpt-5";

    /// <summary>回應逾時時間（秒）</summary>
    public int ResponseTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// 系統提示詞 — 在每個新 Session 的第一則訊息前注入，
    /// 用於指示 AI Agent 行為（例如多媒體輸出格式）。
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;
}
