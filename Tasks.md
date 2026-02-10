# 📋 AI IM Connector — 計畫管理表

> 最後更新：2026-02-10

## 📊 總覽

| 階段 | 說明 | 狀態 | 進度 |
|------|------|------|------|
| 第一階段 | 專案基礎建設 | ✅ 完成 | 4/4 |
| 第二階段 | 核心資料模型 | ✅ 完成 | 4/4 |
| 第三階段 | ACP 客戶端 | ✅ 完成 | 3/3 |
| 第四階段 | 對話管理 | ✅ 完成 | 3/3 |
| 第五階段 | 多媒體處理 | ✅ 完成 | 2/2 |
| 第六階段 | LINE 適配器 | ✅ 完成 | 4/4 |
| 第七階段 | Telegram 適配器 | ✅ 完成 | 3/3 |
| 第八階段 | 訊息路由 | ✅ 完成 | 1/1 |
| 第九階段 | 中介層與安全 | ✅ 完成 | 3/3 |
| 第十階段 | Docker 部署 | ✅ 完成 | 3/3 |
| 第十一階段 | 文件與測試 | ✅ 完成 | 2/2 |
| 後續擴充 | Teams / Google Chat / Slack 等 | 📌 待規劃 | 0/8 |

---

## 第一階段：專案基礎建設

- [x] **1.1** 建立 .NET 8 WebAPI 專案結構（Solution、Project、.gitignore）
- [x] **1.2** 加入 NuGet 套件相依性
  - `Telegram.Bot` — Telegram Bot API
  - `LineBotSDK` — LINE Messaging API
  - `Microsoft.Extensions.Http` — HttpClient 管理
  - `Moq` — 單元測試 Mock 框架
- [x] **1.3** 建立設定檔結構（appsettings.json）含 IM 綁定 Agent 配置
- [x] **1.4** 建立 Configuration 模型類別（ImSettings、AcpSettings、AgentBindingSettings）

## 第二階段：核心資料模型

- [x] **2.1** 定義 `UnifiedMessage` 統一訊息格式（跨平台抽象層）
- [x] **2.2** 定義 `MediaContent` 多媒體內容模型（圖片/影片/音訊/檔案）
- [x] **2.3** 定義 `SessionContext` 對話上下文模型
- [x] **2.4** 定義 ACP JSON-RPC 2.0 訊息模型

## 第三階段：ACP 客戶端

- [x] **3.1** 定義 `IAcpClient` 介面（初始化、發送提示、接收回應、取消）
- [x] **3.2** 實作 `AcpClient`（HTTP 請求 + SSE 串流接收回應）
  - `session/initialize` — 初始化連線
  - `session/new` — 建立新對話
  - `session/prompt` — 發送使用者訊息
  - `session/update` — 接收 AI 回應（SSE 串流）
  - `session/cancel` — 取消處理
- [x] **3.3** 實作 `AcpSessionManager`（管理與 ACP Server 的 Session 生命週期）

## 第四階段：對話管理

- [x] **4.1** 定義 `ISessionStore` 介面
- [x] **4.2** 實作 `InMemorySessionStore`（使用 ConcurrentDictionary）
- [x] **4.3** 實作 `SessionService`（建立/取得/清除 Session、逾時自動清理）

## 第五階段：多媒體處理

- [x] **5.1** 定義 `IMediaHandler` 介面
- [x] **5.2** 實作 `MediaHandler`
  - 從 IM 平台下載多媒體檔案
  - 轉換為 Base64 或暫存檔路徑
  - 回傳多媒體給 IM 平台（從 URL 或 Base64 發送）

## 第六階段：IM 適配器 — LINE

- [x] **6.1** 實作 `LineAdapter`（IImAdapter 介面實作）
- [x] **6.2** 實作 `LineWebhookController`（接收 LINE Webhook 事件）
- [x] **6.3** 實作 `LineMessageConverter`（LINE 訊息 ↔ UnifiedMessage 轉換）
- [x] **6.4** LINE Webhook 簽名驗證（HMAC-SHA256）

## 第七階段：IM 適配器 — Telegram

- [x] **7.1** 實作 `TelegramAdapter`（IImAdapter 介面實作）
- [x] **7.2** 實作 `TelegramWebhookController`（接收 Telegram Webhook）
- [x] **7.3** 實作 `TelegramMessageConverter`（Telegram 訊息 ↔ UnifiedMessage 轉換）

## 第八階段：訊息路由

- [x] **8.1** 實作 `MessageRouter`
  - 根據設定檔中 IM → Agent 綁定，將訊息路由到正確的 ACP Agent
  - 整合 Session 管理（附帶對話歷史）
  - 整合多媒體處理
  - 使用者指令系統（/clear、/help、/status）

## 第九階段：中介層與安全

- [x] **9.1** 實作 `WebhookValidationMiddleware`（Webhook 請求日誌 + Body 緩衝）
- [x] **9.2** 實作 `ExceptionHandlingMiddleware`（全域例外處理與統一錯誤格式）
- [x] **9.3** 設定 DI（Dependency Injection）容器註冊 + Session 清理背景服務

## 第十階段：Docker 部署

- [x] **10.1** 撰寫多階段 Dockerfile（SDK build → ASP.NET runtime）
- [x] **10.2** 撰寫 docker-compose.yml（含環境變數配置 + host.docker.internal）
- [x] **10.3** 建立 .dockerignore

## 第十一階段：文件與測試

- [x] **11.1** 撰寫 README.md（含架構圖、設定說明、部署指南、指令說明）
- [x] **11.2** 撰寫單元測試（30 個測試全部通過）
  - SessionService 測試（5 個）
  - MediaHandler 測試（11 個）
  - UnifiedMessage 測試（6 個）
  - LINE MessageConverter 測試（4 個）
  - Telegram MessageConverter 測試（4 個）

---

## 後續擴充（待規劃）

- [ ] **E.1** Microsoft Teams 適配器
- [ ] **E.2** Google Chat 適配器
- [ ] **E.3** Slack 適配器
- [ ] **E.4** Redis / 資料庫持久化 Session Store
- [ ] **E.5** 多租戶支援
- [ ] **E.6** 訊息佇列（RabbitMQ / Kafka）
- [ ] **E.7** 健康檢查與監控端點（OpenTelemetry）
- [ ] **E.8** Rate Limiting

---

## 🔧 技術棧

| 技術 | 說明 |
|------|------|
| .NET 8 | WebAPI 框架 |
| ACP (HTTP+SSE) | Agent Client Protocol，JSON-RPC 2.0 |
| LineBotSDK | LINE Messaging API |
| Telegram.Bot | Telegram Bot API |
| Docker | 多階段建置容器化部署 |
| xUnit + Moq | 單元測試框架 |

## 📂 關鍵檔案對照

| 工作項目 | 對應檔案 |
|---------|---------|
| 1.1 專案結構 | `AI-IM-Connector.sln`, `.gitignore` |
| 1.3 設定檔 | `appsettings.json`, `appsettings.Development.json` |
| 1.4 設定模型 | `Configuration/ImSettings.cs`, `AcpSettings.cs`, `AgentBindingSettings.cs` |
| 2.1 統一訊息 | `Models/UnifiedMessage.cs` |
| 2.2 多媒體模型 | `Models/MediaContent.cs` |
| 2.3 對話上下文 | `Models/SessionContext.cs` |
| 2.4 ACP 訊息 | `Models/AcpMessages.cs` |
| 3.1-3.3 ACP 客戶端 | `Services/Acp/IAcpClient.cs`, `AcpClient.cs`, `AcpSessionManager.cs` |
| 4.1-4.3 Session 管理 | `Services/Session/ISessionStore.cs`, `InMemorySessionStore.cs`, `SessionService.cs` |
| 5.1-5.2 多媒體處理 | `Services/Media/IMediaHandler.cs`, `MediaHandler.cs` |
| 6.1-6.4 LINE 適配器 | `Adapters/Line/LineAdapter.cs`, `LineWebhookController.cs`, `LineMessageConverter.cs` |
| 7.1-7.3 Telegram 適配器 | `Adapters/Telegram/TelegramAdapter.cs`, `TelegramWebhookController.cs`, `TelegramMessageConverter.cs` |
| 8.1 訊息路由 | `Services/MessageRouter.cs` |
| 9.1-9.2 中介層 | `Middleware/ExceptionHandlingMiddleware.cs`, `WebhookValidationMiddleware.cs` |
| 9.3 DI 註冊 | `Program.cs` |
| 10.1-10.3 Docker | `Dockerfile`, `docker-compose.yml`, `.dockerignore` |
| 11.1 文件 | `README.md` |
| 11.2 測試 | `tests/AiImConnector.Tests/` |
