# AI IM Connector

> 🤖 即時通訊平台 × AI Agent 訊息轉送服務

一個 .NET 8 WebAPI 服務，作為即時通訊平台（IM）與 AI Agent 之間的訊息轉送橋樑。使用者可以在 LINE、Telegram 等 IM 平台上以自然語言與後端 AI 對談，支援文字與多媒體訊息。

## 🏗️ 系統架構

```
┌─────────────┐    Webhook     ┌──────────────────────────┐     ACP (HTTP+SSE)     ┌─────────────────┐
│   LINE      │───────────────▶│                          │────────────────────────▶│ Copilot CLI     │
│   Telegram  │◀───────────────│   AI IM Connector        │◀────────────────────────│ Codex CLI       │
│   (其他 IM) │    Reply API   │   (.NET 8 WebAPI)        │     JSON-RPC 2.0       │ Gemini CLI      │
└─────────────┘                │                          │                        └─────────────────┘
                               └──────────────────────────┘
```

## ✨ 功能特色

- **多平台支援**：LINE、Telegram（Teams、Google Chat、Slack 後續擴充）
- **多媒體訊息**：圖片、影片、音訊、檔案
- **ACP 協定**：透過 Agent Client Protocol 連接 AI Agent
- **對話上下文**：維持 Session，AI 記住先前的對話內容
- **Agent 綁定**：設定檔配置每個 IM 平台對應的 Agent
- **指令系統**：`/clear`、`/help`、`/status`
- **Docker 部署**：容器化建置與部署

## 📋 前置需求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://docs.docker.com/get-docker/)（部署用）
- LINE Channel Access Token & Channel Secret
- Telegram Bot Token
- ACP Server（Copilot CLI / Codex CLI / Gemini CLI）

## 🚀 快速開始

### 本機開發

```bash
# 1. 複製專案
git clone <repository-url>
cd AI-IM-Connector

# 2. 設定密鑰（使用 User Secrets）
cd src/AiImConnector
dotnet user-secrets set "Im:Line:ChannelAccessToken" "YOUR_TOKEN"
dotnet user-secrets set "Im:Line:ChannelSecret" "YOUR_SECRET"
dotnet user-secrets set "Im:Telegram:BotToken" "YOUR_BOT_TOKEN"

# 3. 啟動
dotnet run

# 4. 測試健康檢查
curl http://localhost:5000/health
```

### Docker 部署

```bash
# 1. 設定環境變數
export LINE_CHANNEL_ACCESS_TOKEN="your_token"
export LINE_CHANNEL_SECRET="your_secret"
export TELEGRAM_BOT_TOKEN="your_bot_token"

# 2. 建置並啟動
docker compose up -d

# 3. 查看日誌
docker compose logs -f
```

## ⚙️ 設定說明

### appsettings.json

```json
{
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
    "DefaultServerUrl": "http://localhost:8080",
    "ConnectionTimeoutSeconds": 30,
    "StreamTimeoutSeconds": 120
  },
  "AgentBindings": {
    "Bindings": {
      "Line": {
        "AgentName": "copilot-cli",
        "AcpServerUrl": "http://localhost:8080"
      },
      "Telegram": {
        "AgentName": "gemini-cli",
        "AcpServerUrl": "http://localhost:8081"
      }
    }
  }
}
```

### 環境變數

所有設定都可以透過環境變數覆蓋，使用 `__` 作為階層分隔符號：

| 環境變數 | 說明 |
|---------|------|
| `Im__Line__ChannelAccessToken` | LINE Channel Access Token |
| `Im__Line__ChannelSecret` | LINE Channel Secret |
| `Im__Telegram__BotToken` | Telegram Bot Token |
| `Im__Telegram__SecretToken` | Telegram Webhook Secret Token |
| `Acp__DefaultServerUrl` | ACP Server 預設 URL |
| `AgentBindings__Bindings__Line__AgentName` | LINE 綁定的 Agent 名稱 |
| `AgentBindings__Bindings__Line__AcpServerUrl` | LINE 綁定的 ACP Server URL |

## 🔌 Webhook 端點

| 平台 | 端點 | 說明 |
|------|------|------|
| LINE | `POST /api/webhook/line` | LINE Messaging API Webhook |
| Telegram | `POST /api/webhook/telegram` | Telegram Bot API Webhook |
| 健康檢查 | `GET /health` | 服務健康狀態 |

## 📂 專案結構

```
src/AiImConnector/
├── Models/           # 資料模型（UnifiedMessage、MediaContent、ACP 訊息）
├── Services/         # 核心服務
│   ├── Acp/          # ACP 客戶端（HTTP+SSE）
│   ├── Session/      # 對話管理（InMemory）
│   └── Media/        # 多媒體處理
├── Adapters/         # IM 平台適配器
│   ├── Line/         # LINE 適配器
│   ├── Telegram/     # Telegram 適配器
│   └── _Template/    # 擴充用模板
├── Configuration/    # 設定模型
└── Middleware/       # 中介層
```

## 🤖 使用者指令

| 指令 | 說明 |
|------|------|
| `/clear` | 清除對話歷史 |
| `/help` | 顯示可用指令 |
| `/status` | 查看目前連線狀態 |

## 🔧 擴充新 IM 平台

請參考 `src/AiImConnector/Adapters/_Template/README.md` 中的說明。

## 📄 授權

MIT License
