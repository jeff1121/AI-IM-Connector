# 📋 AI IM Connector — 計畫管理表

> 最後更新：2026-03-24（v0.2.5）

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
| 第十三階段 | 多媒體傳送改為臨時 URL 文字連結 | ✅ 完成 | 1/1 |
| 第十四階段 | 程式碼審查與安全性掃描（v2） | ✅ 完成 | 4/4 |
| 第十五階段 | 程式碼審查（v3） | ✅ 完成 | 3/3 |
| 第十六階段 | 基礎設施強化 | 📌 待開始 | 0/4 |
| 第十七階段 | Slack 適配器 | 📌 待開始 | 0/4 |
| 第十八階段 | Microsoft Teams 適配器 | 📌 待開始 | 0/4 |
| 第十九階段 | Google Chat 適配器 | 📌 待開始 | 0/4 |
| 第二十階段 | 多租戶與進階功能 | 📌 待開始 | 0/5 |

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
- [x] **12.6** 更新 `MessageRouter`（回傳 `RouterResponse`、本機路徑偵測自動重試、Base64 暫存轉 URL、多媒體 URL 附加於文字回應）、兩個 Webhook Controller（分離文字 + 多媒體回應）

---

## 第十三階段：多媒體傳送改為臨時 URL 文字連結

- [x] **13.1** 更新 `MessageRouter` — 多媒體不再透過 IM 原生推送 API 傳送，改將暫存 URL 附加於文字回應，使用者點擊連結即可檢視圖片，相容性更高

---

## 第十四階段：程式碼審查與安全性掃描（v2）

- [x] **14.1** 程式碼品質修復 — Session 資源洩漏修復（stale session 重建時正確 DisposeAsync）、System Prompt 競態條件修復（TryAdd 原子操作）、ClearSession 時清理 SemaphoreSlim、Session 重建後正確標記 System Prompt 已注入
- [x] **14.2** 安全漏洞修復（High）— Telegram Webhook 驗證強化（fail-closed + 常數時間比較）、多媒體下載大小限制（50 MB）、暫存服務容量上限（1000 筆 / 500 MB）
- [x] **14.3** 安全漏洞修復（Medium）— Production 錯誤訊息完整遮蔽（含 400 錯誤）、ReDoS 防護（所有 Regex 加入 1 秒逾時）、LINE Webhook 請求大小限制（1 MB）、加密安全隨機數產生暫存 ID
- [x] **14.4** 基礎設施安全強化 — Caddy 安全標頭（HSTS、CSP、Permissions-Policy）、.env.example 移除真實基礎設施資訊

---

## 第十五階段：程式碼審查（v3）

- [x] **15.1** [High] MediaHostingService TOCTOU 競態條件修復 — 容量檢查與新增操作加入 `lock` 確保原子性，防止並行請求超出暫存上限
- [x] **15.2** [Medium] AiResponseParser ReDoS 防護補全 — 將 inline `Regex.Replace` 改為預編譯 `ExcessNewlinesRegex` 並加入 1 秒逾時，與類別中其他 Regex 一致
- [x] **15.3** [Low] CopilotClientService 啟動執行緒安全 — `StartAsync` 加入 `SemaphoreSlim` 防止並行呼叫建立多個 `CopilotClient`

---

## 第十六階段：基礎設施強化

> 建議優先實作：為後續新增平台與多租戶功能打下穩固基礎。

- [ ] **16.1** 健康檢查與監控端點（OpenTelemetry）
  - 整合 `OpenTelemetry.Extensions.Hosting`、`OpenTelemetry.Instrumentation.AspNetCore`
  - 新增 `/health` 結構化健康檢查（檢查 Copilot CLI 連線狀態、各 IM 平台 API 可用性）
  - 新增 `/metrics` Prometheus 端點（請求數、回應延遲、Session 數量、多媒體暫存使用量）
  - 分散式追蹤（Trace ID 貫穿 Webhook → MessageRouter → Copilot SDK）
- [ ] **16.2** Rate Limiting
  - 使用 .NET 8 內建 `Microsoft.AspNetCore.RateLimiting`
  - 按使用者 ID 限流（防止單一使用者濫用）
  - 按 IM 平台限流（Webhook 端點獨立限流策略）
  - 429 回應包含 `Retry-After` 標頭
- [ ] **16.3** Redis / 資料庫持久化 Session Store
  - 定義 `ISessionStore` 介面（取代 `ConcurrentDictionary` 記憶體快取）
  - 實作 `RedisSessionStore`（使用 `StackExchange.Redis`）
  - 實作 `InMemorySessionStore`（保留現有行為作為預設/開發用）
  - Session 序列化/反序列化（含 CopilotSession 狀態重建）
  - MediaHostingService 改為 Redis 後端（支援多實例部署）
- [ ] **16.4** 訊息佇列（RabbitMQ / Kafka）
  - 定義 `IMessageQueue` 介面
  - 將 Webhook Controller 的 fire-and-forget `Task.Run` 改為佇列消費模式
  - 實作 `RabbitMqMessageQueue`（使用 `RabbitMQ.Client`）
  - 消費者端錯誤重試與死信佇列（DLQ）

## 第十七階段：Slack 適配器

> 相依：第十六階段（建議完成 16.1、16.2 後再開始）

- [ ] **17.1** 安裝 `SlackNet` NuGet 套件，新增 `SlackSettings` 設定類別
- [ ] **17.2** 實作 `SlackAdapter`（`IImAdapter` 介面）— 使用 Web API 發送訊息，支援 Block Kit 富文字格式
- [ ] **17.3** 實作 `SlackWebhookController` — Events API 端點、URL Verification 挑戰回應、Request Signing 驗證（HMAC-SHA256）
- [ ] **17.4** 實作 `SlackMessageConverter` — Slack 事件格式 ↔ `UnifiedMessage` 轉換

## 第十八階段：Microsoft Teams 適配器

> 相依：第十六階段（建議完成 16.1、16.2 後再開始）

- [ ] **18.1** 安裝 `Microsoft.Bot.Builder.Integration.AspNet.Core` NuGet 套件，新增 `TeamsSettings` 設定類別
- [ ] **18.2** 實作 `TeamsAdapter`（`IImAdapter` 介面）— 透過 Bot Framework SDK 發送訊息，支援 Adaptive Cards
- [ ] **18.3** 實作 `TeamsWebhookController` — Bot Framework `/api/messages` 端點、JWT Token 驗證
- [ ] **18.4** 實作 `TeamsMessageConverter` — Bot Framework Activity ↔ `UnifiedMessage` 轉換

## 第十九階段：Google Chat 適配器

> 相依：第十六階段（建議完成 16.1、16.2 後再開始）

- [ ] **19.1** 安裝 `Google.Apis.HangoutsChat.v1` NuGet 套件，新增 `GoogleChatSettings` 設定類別
- [ ] **19.2** 實作 `GoogleChatAdapter`（`IImAdapter` 介面）— 使用 Google Chat API 發送訊息，支援 Cards V2
- [ ] **19.3** 實作 `GoogleChatWebhookController` — HTTP 端點、Google Service Account JWT 驗證
- [ ] **19.4** 實作 `GoogleChatMessageConverter` — Google Chat 事件 ↔ `UnifiedMessage` 轉換

## 第二十階段：多租戶與進階功能

> 相依：第十六階段（需完成 16.3 Redis Session Store）

- [ ] **20.1** 多租戶資料模型 — 定義 `Tenant` 實體（租戶 ID、API Key、允許的平台清單、Agent 綁定覆寫）
- [ ] **20.2** 租戶認證中介層 — API Key 驗證、租戶隔離（Session Key 加入租戶 ID 前綴）
- [ ] **20.3** 租戶管理 API — CRUD 端點（`/api/admin/tenants`），含 API Key 輪換
- [ ] **20.4** 動態平台配置 — 每個租戶可獨立設定 IM 平台 Token 與 Agent 綁定，無需重啟服務
- [ ] **20.5** 使用量統計與計費基礎 — 按租戶記錄訊息數、API 呼叫數、多媒體流量

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
| — Copilot 開發指引 | `.github/copilot-instructions.md` |

---

## 📝 變更紀錄

| 日期 | 說明 |
|------|------|
| 2026-03-24 | **v0.2.5**：程式碼審查（v3）— 修復 3 項問題（MediaHostingService TOCTOU 競態條件、AiResponseParser ReDoS 防護補全、CopilotClientService 啟動執行緒安全）、新增 `.github/copilot-instructions.md`、演進計畫（第十六～二十階段）、47 個測試全通過 |
| 2026-02-24 | **v0.2.4**：程式碼審查與安全性掃描（v2）— 修復 4 項程式碼品質問題（Session 資源洩漏、System Prompt 競態條件、SemaphoreSlim 累積、重建後重複注入）與 12 項安全漏洞（Telegram 驗證強化、多媒體大小限制、暫存容量上限、加密隨機 ID、ReDoS 防護、錯誤訊息遮蔽、請求大小限制、Caddy 安全標頭、.env.example 清理）、47 個測試全通過 |
| 2026-02-13 | **v0.2.3**：修復 Base64 FormatException — 清理 base64 資料中的空白字元 |
| 2026-02-12 | **v0.2.2**：多媒體傳送改為臨時 URL 文字連結 — 不再依賴 IM 平台原生多媒體推送 API，改將暫存 URL 附加於文字回應 |
| 2026-02-12 | **v0.2.1**：AI 多媒體直傳功能 — AiResponseParser、本機路徑修正、System Prompt 注入、MediaHostingService、MediaController、45 個測試全通過 |
| 2026-02-12 | **v0.1.2**：修復 per-platform `ResponseTimeoutSeconds` 未正確覆蓋全域設定的問題 |
| 2026-02-12 | **v0.1.1**：修復 .sln 專案路徑、stale session 偵測與自動重建邏輯 |
| 2026-02-12 | **v0.1.0**：程式碼審查 & 安全性掃描：修復 7 項安全漏洞與程式品質問題 |
| 2026-02-11 | 重構為官方 Copilot SDK（CliUrl 連線外部 server），端對端測試通過 |
| 2026-02-10 | 初始實作完成（11 個階段，30 測試通過） |