namespace AiImConnector.Configuration;

/// <summary>
/// Agent 綁定設定 — 定義每個 IM 平台對應的 ACP Agent
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
    /// <summary>ACP Agent 名稱（例如 copilot-cli、codex-cli、gemini-cli）</summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>ACP Server URL</summary>
    public string AcpServerUrl { get; set; } = string.Empty;
}
