# 📋 AI IM Connector — 計畫管理表

> 最後更新：2026-02-12（v0.2.1）

## 📊 總覽

| 階段 | 說明 | 狀態 | 進度 |
|------|------|------|------|
| 第一階段 | 專案基礎建設 | ✅ 完成 | 4/4 |
| 第二階段 | 核心資料模型 | ✅ 完成 | 3/3 |
| 第三階段 | Copilot SDK 整合（ACP 客戶端） | ✅ 完成 | 8/8 |
| 第四階段 | 對話管理（由 SDK 原生處理） | ✅ 完成 | — |
| 第五階段 | 多媒體處理 | ✅ 完成 | 2/2 |
| 第六階段 | LINE 適配器 | ✅ 完成 | 4/4 |
| 第七階段 | Telegram 適配器 | ✅ 完成 | 3/3 |
| 第八階段 | 訊息路由 | ✅ 完成 | 1/1 |
| 第九階段 | 中介層與安全 | ✅ 完成 | 3/3 |
| 第十階段 | Docker 部署 | ✅ 完成 | 3/3 |
| 第十一階段 | 文件與測試 | ✅ 完成 | 2/2 |
| 第十二階段 | AI 多媒體直傳（繪圖自動傳送） | ✅ 完成 | 6/6 |
| 後續擴充 | Teams / Google Chat / Slack 等 | 📌 待規劃 | 0/8 |

---

## 第一階段：專案基礎建設

- [x] **1.1** 建立 .NET 8 WebAPI 專案結構（Solution、Project、.gitignore）
- [x] **1.2** 加入 NuGet 套件相依性
  - `GitHub.Copilot.SDK` — Copilot CLI 封裝（ACP 協定）
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

## 第三階段：Copilot SDK 整合

- [x] **3.1** 安裝 `GitHub.Copilot.SDK` NuGet 套件 (v0.1.24-preview.0)
- [x] **3.2** 研究 SDK API（CopilotClient、CopilotSession、SessionConfig、MessageOptions）
- [x] **3.3** 定義 `ICopilotClientService` 介面（啟動/停止、建立 Session）
- [x] **3.4** 實作 `CopilotClientService`（封裝 CopilotClient，CliUrl 連線外部 ACP Server）
- [x] **3.5** 實作 `CopilotSessionManager`（使用者 Session 快取 + SDK CopilotSession）
- [x] **3.6** 更新 Configuration 模型（CliUrl、CliPath、ResponseTimeoutSeconds）
- [x] **3.7** 實作 `CopilotLifecycleService`（BackgroundService，隨應用程式啟停 SDK）
- [x] **3.8** 註冊 DI 容器（ICopilotClientService → Singleton、CopilotSessionManager → Singleton）

## 第四階段：對話管理（由 Copilot SDK 原生處理）

> Session 管理由 Copilot SDK 的 `CopilotSession` 原生處理，
> 支援 `CreateSession` / `ResumeSession` / `DeleteSession`，
> 不再需要自建 SessionStore。

## 第五階段：多媒體處理

- [x] **5.1** 定義 `IMediaHandler` 介面（下載、描述產生、類型判斷）
- [x] **5.2** 實作 `MediaHandler`
  - 從 IM 平台下載多媒體檔案並轉換為 Base64
  - 產生多媒體文字描述（`ToMediaDescription`）供 AI 理解
  - 從 MIME 類型判斷多媒體類型

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
  - 根據設定檔中 IM → Agent 綁定，透過 CopilotSessionManager 路由訊息
  - 多媒體內容轉換為文字描述整合至 Prompt
  - 使用者指令系統（/clear、/help、/status）

## 第九階段：中介層與安全

- [x] **9.1** 實作 `WebhookValidationMiddleware`（Webhook 請求日誌 + Body 緩衝）
- [x] **9.2** 實作 `ExceptionHandlingMiddleware`（全域例外處理與統一錯誤格式）
- [x] **9.3** 設定 DI 容器註冊 + CopilotLifecycleService 背景服務

## 第十階段：Docker 部署

- [x] **10.1** 撰寫多階段 Dockerfile（SDK build → ASP.NET runtime，非 root 執行）
- [x] **10.2** 撰寫 docker-compose.yml（含環境變數、Copilot CLI 掛載）
- [x] **10.3** 建立 .dockerignore

## 第十一階段：文件與測試

- [x] **11.1** 撰寫 README.md（含架構圖、訊息流程、設定說明、部署指南）
- [x] **11.2** 撰寫單元測試（45 個測試全部通過）
  - CopilotSessionManager 測試（4 個）— Session ID 產生邏輯
  - MediaHandler 測試（11 個）— 多媒體類型判斷、描述產生
  - UnifiedMessage 測試（6 個）— 指令解析
  - LINE MessageConverter 測試（4 個）— 訊息格式轉換
  - Telegram MessageConverter 測試（4 個）— 訊息格式轉換
  - AiResponseParser 測試（16 個）— AI 回應多媒體解析、本機路徑偵測

---

## 第十二階段：AI 多媒體直傳（繪圖自動傳送）

- [x] **12.1** 建立 `RouterResponse` 模型（文字 + 多媒體 + 本機路徑偵測）
- [x] **12.2** 實作 `AiResponseParser`（從 AI 文字回應中擷取 Markdown 圖片、Base64 Data URI、獨立圖片 URL、本機檔案路徑）
- [x] **12.3** 實作 `MediaHostingService`（Base64 多媒體記憶體暫存、產生公開 URL、10 分鐘 TTL + 自動清理）
- [x] **12.4** 實作 `MediaController`（`GET /api/media/{id}` 端點，供 IM 平台取得檔案）
- [x] **12.5** 擴充 `AgentBinding` 設定（新增 `SystemPrompt` 欄位）、`CopilotSessionManager`（新 Session 首次注入 System Prompt、stale session 重建時清除已注入標記）
- [x] **12.6** 更新 `MessageRouter`（回傳 `RouterResponse`、本機路徑偵測自動重試、Base64 暫存轉 URL）、兩個 Webhook Controller（分離文字 + 多媒體回應）

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

| 技術 | 版本 | 說明 |
|------|------|------|
| .NET | 8.0 | WebAPI 框架 |
| GitHub.Copilot.SDK | 0.1.24-preview.0 | 官方 Copilot CLI SDK，ACP (JSON-RPC) 通訊 |
| LineBotSDK | — | LINE Messaging API |
| Telegram.Bot | — | Telegram Bot API |
| Docker | — | 多階段建置容器化部署 |
| xUnit + Moq | — | 單元測試框架 |

## 📂 關鍵檔案對照

| 工作項目 | 對應檔案 |
|---------|---------|
| 1.1 專案結構 | `AI-IM-Connector.sln`, `.gitignore` |
| 1.3 設定檔 | `appsettings.json`, `appsettings.Development.json` |
| 1.4 設定模型 | `Configuration/ImSettings.cs`, `AcpSettings.cs`, `AgentBindingSettings.cs` |
| 2.1 統一訊息 | `Models/UnifiedMessage.cs` |
| 2.2 多媒體模型 | `Models/MediaContent.cs` |
| 2.3 對話上下文 | `Models/SessionContext.cs` |
| 3.3-3.5 Copilot SDK | `Services/Acp/IAcpClient.cs` → ICopilotClientService |
| | `Services/Acp/AcpClient.cs` → CopilotClientService |
| | `Services/Acp/AcpSessionManager.cs` → CopilotSessionManager |
| 5.1-5.2 多媒體處理 | `Services/Media/IMediaHandler.cs`, `MediaHandler.cs` |
| 6.1-6.4 LINE 適配器 | `Adapters/Line/LineAdapter.cs`, `LineWebhookController.cs`, `LineMessageConverter.cs` |
| 7.1-7.3 Telegram 適配器 | `Adapters/Telegram/TelegramAdapter.cs`, `TelegramWebhookController.cs`, `TelegramMessageConverter.cs` |
| 8.1 訊息路由 | `Services/MessageRouter.cs` |
| 9.1-9.2 中介層 | `Middleware/ExceptionHandlingMiddleware.cs`, `WebhookValidationMiddleware.cs` |
| 9.3 DI + 生命週期 | `Program.cs`（含 CopilotLifecycleService） |
| 10.1-10.3 Docker | `Dockerfile`, `docker-compose.yml`, `.dockerignore` |
| 11.1 文件 | `README.md`, `Tasks.md` |
| 11.2 測試 | `tests/AiImConnector.Tests/` |

---

## 📝 變更紀錄

| 日期 | 說明 |
|------|------|
| 2026-02-10 | 初始實作完成（11 個階段，30 測試通過） |
| 2026-02-10 | 重構 ACP 層為 GitHub Copilot SDK，移除舊 HTTP+SSE 客戶端與自建 Session 管理 |
| 2026-02-11 | 重構為官方 Copilot SDK（CliUrl 連線外部 server），端對端測試通過 |
| 2026-02-12 | **程式碼審查 & 安全性掃描**：修復 7 項安全漏洞與程式品質問題，更新所有文件與註解 |
| 2026-02-12 | **v0.1.1**：修復 .sln 專案路徑（`src\AiImConnector\` → `src\ImConnector\`）、stale session 偵測與自動重建邏輯 |
| 2026-02-12 | **v0.1.2**：修復 per-platform `ResponseTimeoutSeconds` 未正確覆蓋全域設定的問題（預設 120s → 600s），docker-compose.yml 新增各平台獨立逾時設定 || 2026-02-12 | **v0.2.1**：AI 多媒體直傳功能 — 新增 AI 回應多媒體解析器（AiResponseParser）、本機路徑偵測與自動修正、System Prompt 注入、Base64 多媒體暫存服務（MediaHostingService）、多媒體 API 端點（MediaController）、ConnectorSettings 設定、Webhook Controller 擴充支援多媒體回應、45 個測試全通過 |