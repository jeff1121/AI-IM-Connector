# AI IM Connector

> 🤖 即時通訊平台 × AI Agent 訊息轉送服務

| 項目 | 值 |
|------|-----|
| **版本** | `0.2.2` |
| **Docker Image** | `logicalis.azurecr.io/ai-connector/im-connector` |
| **Tags** | `0.2.2`、`latest` |
| **平台** | `linux/amd64`、`linux/arm64` |
| **框架** | `.NET 8.0` |
| **SDK** | `GitHub.Copilot.SDK 0.1.24-preview.0` |

一個 .NET 8 WebAPI 服務，作為即時通訊平台（IM）與 AI Agent 之間的訊息轉送橋樑。
使用者可以在 LINE、Telegram 等 IM 平台上以自然語言與後端 AI 對談，支援文字與多媒體訊息的雙向傳送。
AI 生成的圖片會自動解析並直接傳送至 IM 平台，使用者無需手動下載轉傳。

底層透過 [GitHub Copilot SDK](https://github.com/github/awesome-copilot/tree/main/cookbook/copilot-sdk)
以 ACP（Agent Client Protocol）協定啟動 Copilot CLI 子行程，自動管理對話 Session。

## 🏗️ 系統架構

```
┌─────────────┐                ┌──────────────────────────────────────┐                ┌─────────────────┐
│             │    Webhook     │          AI IM Connector              │   ACP (stdio)  │                 │
│   LINE      │───────────────▶│                                      │───────────────▶│  Copilot CLI    │
│   Telegram  │◀───────────────│   .NET 8 WebAPI                      │◀───────────────│  (子行程)       │
│   (其他 IM) │  Reply/Push API│                                      │ Copilot SDK    │                 │
│             │  ＋多媒體傳送  │  ┌────────────┐  ┌──────────────┐    │                └─────────────────┘
└─────────────┘                │  │MessageRouter│  │ SessionMgr   │    │
                               │  └────────────┘  │ ＋SystemPrompt│   │
                               │  ┌────────────┐  └──────────────┘    │
                               │  │ResponseParser│ ┌──────────────┐   │
                               │  └────────────┘  │MediaHosting   │   │
                               │  ┌────────────┐  └──────────────┘    │
                               │  │IM Adapters │  ┌──────────────┐    │
                               │  └────────────┘  │MediaHandler  │    │
                               │                   └──────────────┘   │
                               └──────────────────────────────────────┘
```

### 訊息流程

1. 使用者在 IM 平台（LINE / Telegram）發送訊息
2. IM 平台透過 Webhook 將訊息推送至本服務
3. **CopilotSessionManager** 在新 Session 首次訊息前自動注入 **System Prompt**（指示 AI 以 base64 嵌入圖片）
4. **MessageRouter** 解析訊息，組合 Prompt（含多媒體描述），透過 Copilot SDK 發送至 AI
5. **AiResponseParser** 從 AI 文字回應中擷取多媒體內容（Markdown 圖片、Base64 Data URI、獨立圖片 URL）
6. 若偵測到 AI 僅回傳本機路徑而未嵌入圖片，自動發送修正指令要求 AI 重新提供
7. **MediaHostingService** 將 Base64 圖片暫存在記憶體（10 分鐘 TTL），產生公開 URL
8. 多媒體臨時 URL 附加於文字回應中，使用者在 IM 點擊連結即可直接檢視圖片（不依賴 IM 平台原生多媒體推送 API）

## ✨ 功能特色

| 功能 | 說明 |
|------|------|
| 🌐 多平台支援 | LINE、Telegram（Teams、Google Chat、Slack 後續擴充） |
| 🖼️ 多媒體雙向傳送 | 支援圖片、影片、音訊、檔案的接收與發送；AI 生成的圖片自動傳送至 IM |
| 🎨 AI 繪圖直傳 | AI 生成的圖片（Base64/URL）自動解析、暫存為臨時 URL，附加於文字回應中供使用者點擊檢視 |
| 📝 System Prompt 注入 | 每個新 Session 自動注入系統提示詞，指示 AI 以 base64 嵌入圖片而非存檔 |
| 🔄 本機路徑自動修正 | 偵測 AI 回應中的本機檔案路徑，自動發送修正指令要求重新提供嵌入圖片 |
| 🤖 Copilot SDK | 透過 `GitHub.Copilot.SDK` 啟動 Copilot CLI 子行程，以 ACP 協定通訊 |
| 💬 對話上下文 | SDK 原生 Session 管理，支援對話持久化與恢復，Stale Session 自動重建 |
| ⚙️ 模型綁定 | 設定檔配置每個 IM 平台使用不同的 AI 模型 |
| 🎯 指令系統 | `/clear`（清除對話）、`/help`（說明）、`/status`（連線狀態） |
| 🐳 Docker 部署 | 多階段建置的容器化部署 |
| 🔒 安全性 | Webhook 簽名驗證（常數時間比較）、SSRF 防護、全域例外處理（Production 隱藏細節）、非 root 容器執行 |

## 📋 前置需求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（或更高版本）
- [Docker](https://docs.docker.com/get-docker/)（部署用）
- [GitHub Copilot](https://docs.github.com/en/copilot) 授權（SDK 會自動下載 Copilot CLI）
- GitHub Token（需具備 Copilot 權限，透過 `GITHUB_TOKEN` 環境變數或 User Secrets 提供）
- LINE Channel Access Token 與 Channel Secret
- Telegram Bot Token

## 🚀 快速開始

### 本機開發

```bash
# 1. 複製專案
git clone <repository-url>
cd AI-IM-Connector

# 2. 還原套件
dotnet restore

# 3. 設定密鑰（使用 User Secrets，避免將敏感資訊寫入程式碼）
cd src/AiImConnector
dotnet user-secrets init
dotnet user-secrets set "Im:Line:ChannelAccessToken" "YOUR_LINE_TOKEN"
dotnet user-secrets set "Im:Line:ChannelSecret" "YOUR_LINE_SECRET"
dotnet user-secrets set "Im:Telegram:BotToken" "YOUR_TELEGRAM_BOT_TOKEN"

# 4. 設定 GitHub Token（Copilot SDK 需要）
export GITHUB_TOKEN="your_github_token"

# 5. 啟動服務
dotnet run

# 6. 驗證服務狀態
curl http://localhost:5000/health
```

> 💡 **提示**：開發階段可使用 [ngrok](https://ngrok.com/) 建立 HTTPS 公開 URL，
> 供 LINE 與 Telegram 的 Webhook 回呼使用。

### 執行測試

```bash
dotnet test
```

### Docker 部署

```bash
# 1. 建立 .env 檔案（或直接 export 環境變數）
cat > .env << EOF
LINE_CHANNEL_ACCESS_TOKEN=your_line_token
LINE_CHANNEL_SECRET=your_line_secret
TELEGRAM_BOT_TOKEN=your_telegram_bot_token
GITHUB_TOKEN=your_github_token
EOF

# 2. 建置並啟動
docker compose up -d

# 3. 查看日誌
docker compose logs -f

# 4. 停止服務
docker compose down
```

## ⚙️ 設定說明

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "AiImConnector": "Debug"
    }
  },
  "Connector": {
    "PublicBaseUrl": ""
  },
  "Im": {
    "Line": {
      "ChannelAccessToken": "",
      "ChannelSecret": ""
    },
    "Telegram": {
      "BotToken": "",
      "SecretToken": ""
    }
  },
  "Acp": {
    "CliPath": "",
    "ResponseTimeoutSeconds": 120
  },
  "AgentBindings": {
    "Bindings": {
      "Line": {
        "AgentName": "copilot-cli",
        "Model": "gpt-5",
        "ResponseTimeoutSeconds": 120,
        "SystemPrompt": "你正在透過即時通訊軟體與使用者對話。當使用者要求生成圖片時，必須使用 Markdown 圖片語法搭配 base64 data URI 格式嵌入回應中..."
      },
      "Telegram": {
        "AgentName": "gemini-cli",
        "Model": "claude-sonnet-4.5",
        "ResponseTimeoutSeconds": 120,
        "SystemPrompt": "..."
      }
    }
  }
}
```

### 設定項目說明

| 區段 | 設定 | 說明 | 預設值 |
|------|------|------|--------|
| `Im:Line` | `ChannelAccessToken` | LINE Messaging API 的 Channel Access Token | — |
| `Im:Line` | `ChannelSecret` | LINE Webhook 簽名驗證用的 Channel Secret | — |
| `Im:Telegram` | `BotToken` | Telegram Bot API Token | — |
| `Im:Telegram` | `SecretToken` | Telegram Webhook 驗證用的 Secret Token（選填） | — |
| `Acp` | `CliPath` | Copilot CLI 執行檔路徑（留空則由 SDK 自動下載） | `""` |
| `Acp` | `ResponseTimeoutSeconds` | AI 回應的全域逾時秒數（建議 Docker 部署設為 `600`） | `120` |
| `Connector` | `PublicBaseUrl` | 連接器公開 URL（供 Base64 多媒體暫存服務使用，例如 `https://yourdomain.com`） | `""` |
| `AgentBindings:Bindings:{平台}` | `AgentName` | Agent 名稱（識別用） | — |
| `AgentBindings:Bindings:{平台}` | `Model` | AI 模型名稱（如 `gpt-5`、`claude-sonnet-4.5`） | `gpt-5` |
| `AgentBindings:Bindings:{平台}` | `ResponseTimeoutSeconds` | 該平台專屬的回應逾時秒數（優先於全域設定） | `120` |
| `AgentBindings:Bindings:{平台}` | `SystemPrompt` | 系統提示詞（新 Session 首次訊息前注入，指示 AI 圖片輸出格式等行為） | `""` |

### 環境變數

所有設定都可以透過環境變數覆蓋（使用 `__` 作為階層分隔符號）：

```bash
# IM 平台設定
Im__Line__ChannelAccessToken=your_token
Im__Line__ChannelSecret=your_secret
Im__Telegram__BotToken=your_bot_token

# Copilot SDK 設定
Acp__CliPath=/path/to/copilot-cli
Acp__ResponseTimeoutSeconds=120

# Agent 綁定
AgentBindings__Bindings__Line__AgentName=copilot-cli
AgentBindings__Bindings__Line__Model=gpt-5
AgentBindings__Bindings__Line__ResponseTimeoutSeconds=600
AgentBindings__Bindings__Line__SystemPrompt=你正在透過即時通訊軟體與使用者對話...
AgentBindings__Bindings__Telegram__Model=claude-sonnet-4.5
AgentBindings__Bindings__Telegram__ResponseTimeoutSeconds=600
AgentBindings__Bindings__Telegram__SystemPrompt=...

# 連接器設定
Connector__PublicBaseUrl=https://yourdomain.com
```

## 🔌 API 端點

| 端點 | 方法 | 說明 |
|------|------|------|
| `/api/webhook/line` | POST | LINE Messaging API Webhook 接收端點 |
| `/api/webhook/telegram` | POST | Telegram Bot API Webhook 接收端點 |
| `/api/media/{id}` | GET | 多媒體暫存檔案存取端點（使用者點擊臨時 URL 即可取得 AI 生成的圖片） |
| `/health` | GET | 服務健康檢查（回傳 `{ status, timestamp }`） |
| `/swagger` | GET | Swagger UI（僅 Development 環境） |

## 📂 專案結構

```
AI-IM-Connector/
├── src/AiImConnector/
│   ├── Program.cs                          # 應用程式進入點 + DI 註冊
│   ├── appsettings.json                    # 設定檔
│   ├── Configuration/                      # 設定模型
│   │   ├── ImSettings.cs                   #   IM 平台設定（LINE、Telegram）
│   │   ├── AcpSettings.cs                  #   Copilot SDK 連線設定
│   │   └── AgentBindingSettings.cs         #   Agent 綁定設定
│   ├── Models/                             # 資料模型
│   │   ├── UnifiedMessage.cs               #   統一訊息格式（跨平台抽象層）
│   │   ├── MediaContent.cs                 #   多媒體內容模型
│   │   ├── RouterResponse.cs               #   AI 回應結果（文字 + 多媒體 + 本機路徑偵測）
│   │   └── SessionContext.cs               #   對話上下文
│   ├── Services/                           # 核心服務
│   │   ├── MessageRouter.cs                #   訊息路由（IM → AI → IM，含多媒體解析與本機路徑修正）
│   │   ├── AiResponseParser.cs             #   AI 回應解析器（擷取 Markdown 圖片、Base64、URL、本機路徑）
│   │   ├── Acp/                            #   Copilot SDK 封裝
│   │   │   ├── IAcpClient.cs               #     ICopilotClientService 介面
│   │   │   ├── AcpClient.cs                #     CopilotClientService 實作
│   │   │   └── AcpSessionManager.cs        #     CopilotSessionManager（Session 管理 + System Prompt 注入）
│   │   └── Media/                          #   多媒體處理
│   │       ├── IMediaHandler.cs            #     多媒體處理介面
│   │       ├── MediaHandler.cs             #     下載、轉換、描述產生
│   │       └── MediaHostingService.cs      #     Base64 多媒體暫存服務（10 分鐘 TTL）
│   ├── Adapters/                           # IM 平台適配器
│   │   ├── IImAdapter.cs                   #   適配器介面（支援文字 + 多媒體回覆）
│   │   ├── Line/                           #   LINE 適配器
│   │   │   ├── LineAdapter.cs              #     LINE 訊息發送
│   │   │   ├── LineWebhookController.cs    #     LINE Webhook 接收
│   │   │   └── LineMessageConverter.cs     #     LINE 訊息格式轉換
│   │   ├── Telegram/                       #   Telegram 適配器
│   │   │   ├── TelegramAdapter.cs          #     Telegram 訊息發送
│   │   │   ├── TelegramWebhookController.cs#     Telegram Webhook 接收
│   │   │   └── TelegramMessageConverter.cs #     Telegram 訊息格式轉換
│   │   └── _Template/                      #   擴充用模板（新增平台參考）
│   │       └── README.md
│   └── Middleware/                         # 中介層
│       ├── ExceptionHandlingMiddleware.cs  #   全域例外處理
│       └── WebhookValidationMiddleware.cs  #   Webhook 請求驗證與日誌
├── tests/AiImConnector.Tests/              # 單元測試（45 個測試）
├── Dockerfile                              # 多階段建置 Dockerfile
├── docker-compose.yml                      # Docker Compose 配置
├── Tasks.md                                # 計畫管理表
└── README.md                               # 本文件
```

## 🤖 使用者指令

在 IM 平台中輸入以下指令：

| 指令 | 說明 |
|------|------|
| `/clear` | 清除目前的對話歷史，開始新的對話 |
| `/help` | 顯示所有可用指令說明 |
| `/status` | 查看目前連線狀態、綁定的 Agent 與模型 |

## 🔧 技術棧

| 技術 | 版本 | 說明 |
|------|------|------|
| .NET | 8.0 | WebAPI 框架 |
| [GitHub.Copilot.SDK](https://www.nuget.org/packages/GitHub.Copilot.SDK) | 0.1.23 | Copilot CLI 封裝，ACP (stdio) 通訊 |
| [LineBotSDK](https://www.nuget.org/packages/LineBotSDK) | — | LINE Messaging API |
| [Telegram.Bot](https://www.nuget.org/packages/Telegram.Bot) | — | Telegram Bot API |
| Docker | — | 多階段建置容器化部署 |
| xUnit + Moq | — | 單元測試框架 |

## 🔌 擴充新 IM 平台

請參考 [`src/AiImConnector/Adapters/_Template/README.md`](src/AiImConnector/Adapters/_Template/README.md) 中的步驟說明。

基本流程：

1. 在 `Adapters/` 下建立新平台資料夾
2. 實作 `IImAdapter` 介面、Webhook Controller、MessageConverter
3. 在 `Configuration/ImSettings.cs` 新增設定類別
4. 在 `Program.cs` 註冊 DI 服務
5. 在 `appsettings.json` 新增設定與 Agent 綁定

## ⚠️ 注意事項

1. **Webhook 需要 HTTPS 公開 URL**：LINE 與 Telegram 的 Webhook 都要求 HTTPS，開發階段可使用 ngrok
2. **GitHub Token 權限**：需確保 Token 具有 Copilot 存取權限
3. **Copilot CLI 自動下載**：SDK 會在首次建置時自動下載 Copilot CLI，無需手動安裝
4. **多媒體限制**：各 IM 平台有不同的檔案大小限制，MediaHandler 會處理相關驗證
5. **Secret 管理**：所有敏感資訊（Token、Secret）請使用環境變數或 User Secrets，切勿寫入程式碼

## 🔒 安全性措施

| 項目 | 說明 |
|------|------|
| Webhook 簽名驗證 | LINE 使用 HMAC-SHA256 常數時間比較（`CryptographicOperations.FixedTimeEquals`），防止 timing attack |
| Telegram 驗證 | 支援 Secret Token 驗證請求來源合法性 |
| SSRF 防護 | MediaHandler 限制多媒體下載 URL 僅允許 IM 平台官方 API（白名單機制） |
| 例外資訊保護 | ExceptionHandlingMiddleware 在 Production 環境隱藏內部錯誤細節 |
| System Prompt 注入 | 新 Session 首次訊息前自動注入系統提示詞，指示 AI 以 base64 data URI 嵌入圖片，避免存在本機路徑 |
| 本機路徑偵測與自動修正 | 偵測 AI 回應中的本機檔案路徑，自動發送修正指令要求 AI 重新提供嵌入圖片 |
| 多媒體暫存服務 | Base64 圖片暫存在記憶體（10 分鐘 TTL），產生臨時 URL 附加於文字回應中供使用者點擊 |
| 執行緒安全 | CopilotSessionManager 使用 per-user SemaphoreSlim 防止並行建立重複 Session |
| Stale Session 重建 | 自動偵測 "Session not found" 錯誤，清除快取並重建新 Session（容器重啟恢復力） |
| 逾時控制 | 支援 per-platform `ResponseTimeoutSeconds` 覆蓋全域逾時設定，避免長時間回應被截斷 |
| 輸入驗證 | Telegram ChatId 使用安全的 TryParse 解析，避免格式異常 |
| 容器安全 | Docker 以非 root 使用者執行 |
| HTTPS 強制 | Caddy 反向代理自動管理 Let's Encrypt 憑證 |

## � 變更紀錄

| 版本 | 日期 | 說明 |
|------|------|------|| 0.2.2 | 2026-02-12 | 多媒體傳送改為臨時 URL 文字連結：不再依賴 IM 平台原生多媒體推送 API，改將暫存 URL 附加於文字回應，使用者點擊連結即可檢視圖片，相容性更高 |
| 0.2.1 | 2026-02-12 | AI 繪圖多媒體直傳功能：System Prompt 注入、AI 回應多媒體解析（AiResponseParser）、本機路徑自動修正、Base64 圖片暫存服務（MediaHostingService）、多媒體 API 端點（MediaController）、45 個測試全通過 || 0.1.2 | 2026-02-12 | 修復 stale session 自動重建、per-platform 逾時設定覆蓋修正、RESPONSE_TIMEOUT 預設值調升至 600 秒 |
| 0.1.1 | 2026-02-12 | 修復 .sln 專案路徑、stale session 偵測邏輯 |
| 0.1.0 | 2026-02-12 | 程式碼審查 & 安全性掃描：修復 7 項安全漏洞，更新文件與註解 |
| 0.0.1 | 2026-02-11 | 初始版本：LINE + Telegram 雙平台支援、Copilot SDK 整合、完整測試覆蓋 |

## �📄 授權

MIT License
