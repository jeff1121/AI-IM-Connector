using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AiImConnector.Configuration;
using AiImConnector.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiImConnector.Services.Acp;

/// <summary>
/// ACP 客戶端實作 — 透過 HTTP + SSE 與 ACP Server 通訊
/// </summary>
public class AcpClient : IAcpClient
{
    private readonly HttpClient _httpClient;
    private readonly AcpSettings _settings;
    private readonly ILogger<AcpClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AcpClient(HttpClient httpClient, IOptions<AcpSettings> settings, ILogger<AcpClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.ConnectionTimeoutSeconds);
    }

    /// <inheritdoc />
    public async Task<AcpResponse> InitializeAsync(string serverUrl, CancellationToken cancellationToken = default)
    {
        var request = new AcpRequest
        {
            Method = "session/initialize",
            Params = new AcpInitializeParams()
        };

        _logger.LogInformation("正在初始化 ACP 連線：{ServerUrl}", serverUrl);
        return await SendRequestAsync(serverUrl, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AcpResponse> CreateSessionAsync(string serverUrl, CancellationToken cancellationToken = default)
    {
        var request = new AcpRequest
        {
            Method = "session/new"
        };

        _logger.LogInformation("正在建立新的 ACP Session：{ServerUrl}", serverUrl);
        return await SendRequestAsync(serverUrl, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> SendPromptAsync(string serverUrl, AcpPromptParams promptParams, CancellationToken cancellationToken = default)
    {
        var request = new AcpRequest
        {
            Method = "session/prompt",
            Params = promptParams
        };

        _logger.LogDebug("發送 Prompt 到 ACP Server：{ServerUrl}，SessionId：{SessionId}", serverUrl, promptParams.SessionId);
        var response = await SendRequestAsync(serverUrl, request, cancellationToken);

        if (!response.IsSuccess)
        {
            _logger.LogError("ACP Prompt 失敗：{ErrorCode} {ErrorMessage}", response.Error?.Code, response.Error?.Message);
            throw new InvalidOperationException($"ACP 錯誤：{response.Error?.Message}");
        }

        return response.Result?.Content ?? string.Empty;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AcpStreamEvent> SendPromptStreamAsync(
        string serverUrl,
        AcpPromptParams promptParams,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new AcpRequest
        {
            Method = "session/prompt",
            Params = promptParams
        };

        var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{serverUrl.TrimEnd('/')}/rpc")
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        _logger.LogDebug("發送串流 Prompt 到 ACP Server：{ServerUrl}", serverUrl);

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? eventType = null;
        var dataBuilder = new StringBuilder();

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;

            if (line.StartsWith("event:"))
            {
                eventType = line[6..].Trim();
            }
            else if (line.StartsWith("data:"))
            {
                dataBuilder.Append(line[5..].Trim());
            }
            else if (string.IsNullOrEmpty(line))
            {
                // 空行表示事件結束
                if (eventType != null || dataBuilder.Length > 0)
                {
                    yield return new AcpStreamEvent
                    {
                        EventType = eventType ?? "message",
                        Data = dataBuilder.ToString()
                    };
                    eventType = null;
                    dataBuilder.Clear();
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task CancelAsync(string serverUrl, string sessionId, CancellationToken cancellationToken = default)
    {
        var request = new AcpRequest
        {
            Method = "session/cancel",
            Params = new { sessionId }
        };

        _logger.LogInformation("取消 ACP Session：{SessionId}", sessionId);
        await SendRequestAsync(serverUrl, request, cancellationToken);
    }

    /// <summary>發送 JSON-RPC 請求到 ACP Server</summary>
    private async Task<AcpResponse> SendRequestAsync(string serverUrl, AcpRequest request, CancellationToken cancellationToken)
    {
        var url = $"{serverUrl.TrimEnd('/')}/rpc";
        var jsonContent = JsonSerializer.Serialize(request, JsonOptions);

        using var httpResponse = await _httpClient.PostAsync(
            url,
            new StringContent(jsonContent, Encoding.UTF8, "application/json"),
            cancellationToken);

        httpResponse.EnsureSuccessStatusCode();

        var response = await httpResponse.Content.ReadFromJsonAsync<AcpResponse>(JsonOptions, cancellationToken);
        return response ?? new AcpResponse { Error = new AcpError { Code = -1, Message = "無法解析 ACP 回應" } };
    }
}
