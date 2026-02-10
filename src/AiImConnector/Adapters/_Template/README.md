# 擴充用模板

此目錄提供新增 IM 平台適配器的參考模板。

## 新增 IM 平台步驟

1. 在 `Adapters/` 下建立新平台的資料夾（例如 `Slack/`）
2. 實作以下檔案：
   - `{Platform}Adapter.cs` — 實作 `IImAdapter` 介面
   - `{Platform}WebhookController.cs` — 接收 Webhook 事件
   - `{Platform}MessageConverter.cs` — 訊息格式轉換

3. 在 `Configuration/ImSettings.cs` 中新增平台設定類別
4. 在 `Program.cs` 中註冊 DI 服務
5. 在 `appsettings.json` 中新增平台設定與 Agent 綁定

## 介面說明

### IImAdapter

```csharp
public interface IImAdapter
{
    string PlatformName { get; }
    Task ReplyTextAsync(UnifiedMessage message, string replyText, CancellationToken ct);
    Task ReplyMediaAsync(UnifiedMessage message, MediaContent media, CancellationToken ct);
}
```
