using System.Text.Json.Serialization;

namespace AiImConnector.Models;

/// <summary>
/// ACP JSON-RPC 2.0 請求訊息
/// </summary>
public class AcpRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

/// <summary>
/// ACP JSON-RPC 2.0 回應訊息
/// </summary>
public class AcpResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("result")]
    public AcpResult? Result { get; set; }

    [JsonPropertyName("error")]
    public AcpError? Error { get; set; }

    /// <summary>是否成功</summary>
    public bool IsSuccess => Error == null;
}

/// <summary>
/// ACP 回應結果
/// </summary>
public class AcpResult
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("capabilities")]
    public AcpCapabilities? Capabilities { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// ACP Agent 能力宣告
/// </summary>
public class AcpCapabilities
{
    [JsonPropertyName("supportsAudio")]
    public bool SupportsAudio { get; set; }

    [JsonPropertyName("supportsImages")]
    public bool SupportsImages { get; set; }

    [JsonPropertyName("supportsFiles")]
    public bool SupportsFiles { get; set; }

    [JsonPropertyName("supportsStreaming")]
    public bool SupportsStreaming { get; set; }
}

/// <summary>
/// ACP 錯誤訊息
/// </summary>
public class AcpError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// ACP SSE 串流事件
/// </summary>
public class AcpStreamEvent
{
    /// <summary>事件類型（token、done、error）</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>事件資料</summary>
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// ACP Session 初始化參數
/// </summary>
public class AcpInitializeParams
{
    [JsonPropertyName("clientInfo")]
    public AcpClientInfo ClientInfo { get; set; } = new();
}

/// <summary>
/// ACP 客戶端資訊
/// </summary>
public class AcpClientInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "AI-IM-Connector";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
}

/// <summary>
/// ACP Prompt 參數
/// </summary>
public class AcpPromptParams
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("context")]
    public List<AcpContextMessage>? Context { get; set; }

    [JsonPropertyName("attachments")]
    public List<AcpAttachment>? Attachments { get; set; }
}

/// <summary>
/// ACP 上下文訊息（對話歷史）
/// </summary>
public class AcpContextMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// ACP 附件（多媒體檔案）
/// </summary>
public class AcpAttachment
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }
}
