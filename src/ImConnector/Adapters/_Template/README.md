# 擴充用模板

此目錄提供新增 IM 平台適配器的參考模板。

## 新增 IM 平台步驟

1. 在 `Adapters/` 下建立新平台的資料夾（例如 `Slack/`）
2. 實作以下檔案：
   - `{Platform}Adapter.cs` — 實作 `IImAdapter` 介面（訊息發送）
   - `{Platform}WebhookController.cs` — 接收 Webhook 事件（HTTP POST 端點）
   - `{Platform}MessageConverter.cs` — 訊息格式轉換（平台訊息 ↔ `UnifiedMessage`）

3. 在 `Configuration/ImSettings.cs` 中新增平台設定類別
4. 在 `Program.cs` 中註冊 DI 服務
5. 在 `appsettings.json` 中新增平台設定與 Agent 綁定

## 介面說明

### IImAdapter

```csharp
public interface IImAdapter
{
    /// <summary>平台名稱（如 "Line"、"Telegram"）</summary>
    string PlatformName { get; }

    /// <summary>回覆文字訊息</summary>
    Task ReplyTextAsync(UnifiedMessage message, string replyText, CancellationToken ct);

    /// <summary>回覆多媒體訊息</summary>
    Task ReplyMediaAsync(UnifiedMessage message, MediaContent media, CancellationToken ct);
}
```

### 設定綁定範例

在 `appsettings.json` 中新增：

```json
{
  "Im": {
    "YourPlatform": {
      "ApiToken": "",
      "WebhookSecret": ""
    }
  },
  "AgentBindings": {
    "Bindings": {
      "YourPlatform": {
        "AgentName": "copilot-cli",
        "Model": "gpt-5",
        "ResponseTimeoutSeconds": 120
      }
    }
  }
}
```

## 現有實作參考

- **LINE 適配器**：`Adapters/Line/` — HMAC-SHA256 簽名驗證、LINE Messaging API
- **Telegram 適配器**：`Adapters/Telegram/` — Telegram Bot API、多媒體檔案下載
