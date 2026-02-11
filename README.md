# AI IM Connector

> 🤖 即時通訊平台 × AI Agent 訊息轉送服務

一個 .NET 8 WebAPI 服務，作為即時通訊平台（IM）與 AI Agent 之間的訊息轉送橋樑。
使用者可以在 LINE、Telegram 等 IM 平台上以自然語言與後端 AI 對談，支援文字與多媒體訊息。

底層透過 [GitHub Copilot SDK](https://github.com/github/awesome-copilot/tree/main/cookbook/copilot-sdk)
以 ACP（Agent Client Protocol）協定啟動 Copilot CLI 子行程，自動管理對話 Session。

## 🏗️ 系統架構

```
┌─────────────┐                ┌──────────────────────────────────┐                ┌─────────────────┐
│             │    Webhook     │        AI IM Connector           │   ACP (stdio)  │                 │
│   LINE      │───────────────▶│                                  │───────────────▶│  Copilot CLI    │
│   Telegram  │◀───────────────│   .NET 8 WebAPI                  │◀───────────────│  (子行程)       │
│   (其他 IM) │    Reply API   │                                  │ Copilot SDK    │                 │
│             │                │  ┌────────────┐  ┌────────────┐  │                └─────────────────┘
└─────────────┘                │  │MessageRouter│  │ SessionMgr │  │
                               │  └────────────┘  └────────────┘  │
                               │  ┌────────────┐  ┌────────────┐  │
                               │  │MediaHandler │  │ IM Adapters│  │
                               │  └────────────┘  └────────────┘  │
                               └──────────────────────────────────┘
```

### 訊息流程

1. 使用者在 IM 平台（LINE / Telegram）發送訊息
2. IM 平台透過 Webhook 將訊息推送至本服務
3. **MessageRouter** 解析訊息，組合 Prompt（含多媒體描述）
4. **CopilotSessionManager** 管理使用者 Session，透過 Copilot SDK 發送至 AI
5. AI 回應經由 IM 適配器回傳至使用者

## ✨ 功能特色

| 功能 | 說明 |
|------|------|
| 🌐 多平台支援 | LINE、Telegram（Teams、Google Chat、Slack 後續擴充） |
| 🖼️ 多媒體訊息 | 支援圖片、影片、音訊、檔案（轉換為文字描述傳送至 AI） |
| 🤖 Copilot SDK | 透過 `GitHub.Copilot.SDK` 啟動 Copilot CLI 子行程，以 ACP 協定通訊 |
| 💬 對話上下文 | SDK 原生 Session 管理，支援對話持久化與恢復 |
| ⚙️ 模型綁定 | 設定檔配置每個 IM 平台使用不同的 AI 模型 |
| 🎯 指令系統 | `/clear`（清除對話）、`/help`（說明）、`/status`（連線狀態） |
| 🐳 Docker 部署 | 多階段建置的容器化部署 |
| 🔒 安全性 | Webhook 簽名驗證、全域例外處理、非 root 容器執行 |

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
        "ResponseTimeoutSeconds": 120
      },
      "Telegram": {
        "AgentName": "gemini-cli",
        "Model": "claude-sonnet-4.5",
        "ResponseTimeoutSeconds": 120
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
| `Acp` | `ResponseTimeoutSeconds` | AI 回應的全域逾時秒數 | `120` |
| `AgentBindings:Bindings:{平台}` | `AgentName` | Agent 名稱（識別用） | — |
| `AgentBindings:Bindings:{平台}` | `Model` | AI 模型名稱（如 `gpt-5`、`claude-sonnet-4.5`） | `gpt-5` |
| `AgentBindings:Bindings:{平台}` | `ResponseTimeoutSeconds` | 該平台專屬的回應逾時秒數 | `120` |

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
AgentBindings__Bindings__Telegram__Model=claude-sonnet-4.5
```

## 🔌 API 端點

| 端點 | 方法 | 說明 |
|------|------|------|
| `/api/webhook/line` | POST | LINE Messaging API Webhook 接收端點 |
| `/api/webhook/telegram` | POST | Telegram Bot API Webhook 接收端點 |
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
│   │   └── SessionContext.cs               #   對話上下文
│   ├── Services/                           # 核心服務
│   │   ├── MessageRouter.cs                #   訊息路由（IM → AI → IM）
│   │   ├── Acp/                            #   Copilot SDK 封裝
│   │   │   ├── IAcpClient.cs               #     ICopilotClientService 介面
│   │   │   ├── AcpClient.cs                #     CopilotClientService 實作
│   │   │   └── AcpSessionManager.cs        #     CopilotSessionManager 對話管理
│   │   └── Media/                          #   多媒體處理
│   │       ├── IMediaHandler.cs            #     多媒體處理介面
│   │       └── MediaHandler.cs             #     下載、轉換、描述產生
│   ├── Adapters/                           # IM 平台適配器
│   │   ├── IImAdapter.cs                   #   適配器介面
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
├── tests/AiImConnector.Tests/              # 單元測試（29 個測試）
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

## 📄 授權

MIT License
