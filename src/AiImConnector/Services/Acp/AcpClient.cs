using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiImConnector.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiImConnector.Services.Acp;

/// <summary>
/// ACP 客戶端實作 — 透過持久 TCP 連線到 ACP Server（Copilot CLI --acp --port）
/// 協定：JSON-RPC 2.0 over TCP，每行一個 JSON 訊息
/// </summary>
public class CopilotClientService : ICopilotClientService
{
    private readonly AcpSettings _settings;
    private readonly ILogger<CopilotClientService> _logger;

    private TcpClient? _tcpClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private Task? _readTask;
    private CancellationTokenSource? _readCts;
    private int _nextId;

    /// <summary>等待回應的 pending requests（id → TaskCompletionSource）</summary>
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pendingRequests = new();

    /// <summary>串流中的 session prompt 回應（sessionId → 累積文字）</summary>
    private readonly ConcurrentDictionary<string, StringBuilder> _streamingResponses = new();

    /// <summary>串流完成信號（sessionId → TaskCompletionSource）</summary>
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _streamingCompletions = new();

    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public bool IsConnected => _tcpClient?.Connected ?? false;

    public CopilotClientService(IOptions<AcpSettings> settings, ILogger<CopilotClientService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        var host = _settings.ServerHost;
        var port = _settings.ServerPort;

        _logger.LogInformation("正在連線到 ACP Server {Host}:{Port}...", host, port);

        _tcpClient = new TcpClient(AddressFamily.InterNetworkV6);
        _tcpClient.Client.DualMode = true; // 同時支援 IPv4 和 IPv6
        await _tcpClient.ConnectAsync(host, port, cancellationToken);

        var stream = _tcpClient.GetStream();
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        // 啟動背景讀取任務
        _readCts = new CancellationTokenSource();
        _readTask = Task.Run(() => ReadLoopAsync(_readCts.Token), _readCts.Token);

        // 發送 initialize
        var initResult = await SendRequestAsync("initialize", new
        {
            protocolVersion = 1,
            capabilities = new { },
            clientInfo = new { name = "AI-IM-Connector", version = "1.0.0" }
        }, cancellationToken);

        var agentName = initResult.TryGetProperty("agentInfo", out var info)
            ? info.GetProperty("name").GetString() : "未知";
        _logger.LogInformation("ACP Server 連線成功，Agent：{Agent}", agentName);
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (_readCts != null)
        {
            await _readCts.CancelAsync();
        }

        _writer?.Dispose();
        _reader?.Dispose();
        _tcpClient?.Dispose();
        _tcpClient = null;

        _logger.LogInformation("ACP Server 連線已關閉");
    }

    /// <inheritdoc />
    public async Task<string> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendRequestAsync("session/new", new
        {
            cwd = Directory.GetCurrentDirectory(),
            mcpServers = Array.Empty<object>()
        }, cancellationToken);

        var sessionId = result.GetProperty("sessionId").GetString()!;
        _logger.LogInformation("建立 ACP Session：{SessionId}", sessionId);
        return sessionId;
    }

    /// <summary>prompt request id → sessionId 的對應（用於偵測完成信號）</summary>
    private readonly ConcurrentDictionary<string, string> _promptRequestSessions = new();

    /// <inheritdoc />
    public async Task<string> SendAndWaitAsync(
        string sessionId, string prompt, string? model = null,
        TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(_settings.ResponseTimeoutSeconds);

        // 準備串流回應收集
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _streamingResponses[sessionId] = new StringBuilder();
        _streamingCompletions[sessionId] = completion;

        // 建構 prompt 參數
        var promptParams = new Dictionary<string, object>
        {
            ["sessionId"] = sessionId,
            ["prompt"] = new[] { new { type = "text", text = prompt } }
        };
        if (!string.IsNullOrEmpty(model))
        {
            promptParams["model"] = model;
        }

        _logger.LogDebug("發送 prompt 到 Session {SessionId}：{Prompt}",
            sessionId, prompt.Length > 100 ? prompt[..100] + "..." : prompt);

        // 發送請求 — 完成信號是帶有相同 id 的 response（result.stopReason）
        var id = Interlocked.Increment(ref _nextId).ToString();
        _promptRequestSessions[id] = sessionId;
        await SendRawAsync(id, "session/prompt", promptParams, cancellationToken);

        // 等待串流完成或逾時
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(effectiveTimeout);

        try
        {
            var registration = cts.Token.Register(() => completion.TrySetCanceled());
            var response = await completion.Task;
            registration.Dispose();

            _logger.LogDebug("收到完整回應（{Length} 字元）", response.Length);
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("等待 AI 回應逾時：{SessionId}", sessionId);
            // 回傳已收到的部分內容
            if (_streamingResponses.TryGetValue(sessionId, out var partial) && partial.Length > 0)
            {
                return partial.ToString();
            }
            throw;
        }
        finally
        {
            _streamingResponses.TryRemove(sessionId, out _);
            _streamingCompletions.TryRemove(sessionId, out _);
        }
    }

    /// <summary>發送 JSON-RPC 請求並等待回應</summary>
    private async Task<JsonElement> SendRequestAsync(string method, object @params, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _nextId).ToString();
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[id] = tcs;

        await SendRawAsync(id, method, @params, cancellationToken);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30));
        var registration = cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            return await tcs.Task;
        }
        finally
        {
            registration.Dispose();
            _pendingRequests.TryRemove(id, out _);
        }
    }

    /// <summary>發送原始 JSON-RPC 訊息</summary>
    private async Task SendRawAsync(string id, string method, object @params, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id,
            method,
            @params
        });

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _writer!.WriteLineAsync(message);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>背景讀取迴圈 — 處理 ACP Server 回傳的所有訊息</summary>
    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await _reader!.ReadLineAsync(cancellationToken);
                if (line == null) break; // 連線中斷

                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var json = JsonSerializer.Deserialize<JsonElement>(line);
                    HandleMessage(json);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "無法解析 ACP 訊息：{Line}", line.Length > 200 ? line[..200] : line);
                }
            }
        }
        catch (OperationCanceledException) { /* 正常停止 */ }
        catch (IOException) { _logger.LogWarning("ACP Server 連線中斷"); }
        catch (Exception ex) { _logger.LogError(ex, "ACP 讀取迴圈發生錯誤"); }
    }

    /// <summary>處理 ACP Server 回傳的訊息</summary>
    private void HandleMessage(JsonElement json)
    {
        // 有 id 的是 response（對應 pending request）
        if (json.TryGetProperty("id", out var idProp))
        {
            var id = idProp.ToString();

            // 檢查是否有 error
            if (json.TryGetProperty("error", out var error))
            {
                var errorMsg = error.TryGetProperty("message", out var msg) ? msg.GetString() : "未知錯誤";
                _logger.LogError("ACP 請求錯誤 [{Id}]：{Error}", id, errorMsg);

                if (_pendingRequests.TryRemove(id, out var errorTcs))
                {
                    errorTcs.TrySetException(new InvalidOperationException($"ACP 錯誤：{errorMsg}"));
                }

                // 如果是 prompt 請求的錯誤，也通知串流完成
                if (_promptRequestSessions.TryRemove(id, out var errSessionId)
                    && _streamingCompletions.TryGetValue(errSessionId, out var errComp))
                {
                    errComp.TrySetException(new InvalidOperationException($"ACP 錯誤：{errorMsg}"));
                }
                return;
            }

            // 成功回應
            if (json.TryGetProperty("result", out var result))
            {
                // 檢查是否為 prompt 請求的完成信號（result.stopReason）
                if (_promptRequestSessions.TryRemove(id, out var sessionId))
                {
                    var stopReason = result.TryGetProperty("stopReason", out var sr) ? sr.GetString() : "unknown";
                    _logger.LogDebug("Session {SessionId} prompt 完成，stopReason：{Reason}", sessionId, stopReason);

                    if (_streamingCompletions.TryGetValue(sessionId, out var comp))
                    {
                        var text = _streamingResponses.TryGetValue(sessionId, out var sb) ? sb.ToString() : "";
                        comp.TrySetResult(text);
                    }
                    return;
                }

                // 一般請求的回應（initialize, session/new）
                if (_pendingRequests.TryRemove(id, out var tcs))
                {
                    tcs.TrySetResult(result);
                }
            }
            return;
        }

        // 沒有 id 的是 notification（串流事件）
        if (json.TryGetProperty("method", out var methodProp))
        {
            var method = methodProp.GetString();
            var @params = json.TryGetProperty("params", out var p) ? p : default;

            switch (method)
            {
                case "session/update":
                    HandleSessionUpdate(@params);
                    break;
                default:
                    _logger.LogDebug("收到 ACP notification：{Method}", method);
                    break;
            }
        }
    }

    /// <summary>處理 session/update notification — 串流 AI 回應</summary>
    private void HandleSessionUpdate(JsonElement @params)
    {
        if (!@params.TryGetProperty("sessionId", out var sidProp)) return;
        var sessionId = sidProp.GetString()!;

        // ACP 格式：params.update.content.text
        if (!@params.TryGetProperty("update", out var update)) return;

        var updateType = update.TryGetProperty("sessionUpdate", out var su) ? su.GetString() : null;

        // agent_message_chunk — 串流文字內容
        if (updateType == "agent_message_chunk" && update.TryGetProperty("content", out var content))
        {
            var text = content.TryGetProperty("text", out var t) ? t.GetString() : null;
            if (text != null && _streamingResponses.TryGetValue(sessionId, out var buffer))
            {
                buffer.Append(text);
                _logger.LogTrace("Session {SessionId} 收到 chunk：{Text}", sessionId,
                    text.Length > 50 ? text[..50] + "..." : text);
            }
        }
        else
        {
            _logger.LogDebug("Session {SessionId} update type：{Type}", sessionId, updateType);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _writeLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
