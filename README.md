# AI IM Connector

> 🤖 即時通訊平台 × AI Agent 訊息轉送服務

一個 .NET 8 WebAPI 服務，作為即時通訊平台（IM）與 AI Agent 之間的訊息轉送橋樑。使用者可以在 LINE、Telegram 等 IM 平台上以自然語言與後端 AI 對談，支援文字與多媒體訊息。

## 🏗️ 系統架構

```
┌─────────────┐    Webhook     ┌──────────────────────────┐     ACP (stdio)        ┌─────────────────┐
│   LINE      │───────────────▶│                          │────────────────────────▶│                 │
│   Telegram  │◀───────────────│   AI IM Connector        │◀────────────────────────│  Copilot CLI    │
│   (其他 IM) │    Reply API   │   (.NET 8 WebAPI)        │  GitHub.Copilot.SDK    │  (子行程)       │
└─────────────┘                │                          │                        └─────────────────┘
                               └──────────────────────────┘
```

## ✨ 功能特色

- **多平台支援**：LINE、Telegram（Teams、Google Chat、Slack 後續擴充）
- **多媒體訊息**：圖片、影片、音訊、檔案
- **GitHub Copilot SDK**：透過 `GitHub.Copilot.SDK` 啟動 Copilot CLI 子行程，以 ACP 協定通訊
- **對話上下文**：SDK 原生 Session 管理，支援對話持久化與恢復
- **Agent 綁定**：設定檔配置每個 IM 平台對應的模型
- **指令系統**：`/clear`、`/help`、`/status`
- **Docker 部署**：容器化建置與部署

## 📋 前置需求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://docs.docker.com/get-docker/)（部署用）
- [GitHub Copilot CLI](https://docs.github.com/en/copilot)（SDK 會自動下載，或可手動指定路徑）
- GitHub Token（具有 Copilot 權限）
- LINE Channel Access Token & Channel Secret
- Telegram Bot Token

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
dotnet user-secrets set "Acp:GithubToken" "YOUR_GITHUB_TOKEN"

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
    "CliPath": "",
    "GithubToken": "",
    "ResponseTimeoutSeconds": 120
  },
  "AgentBindings": {
    "Bindings": {
      "Line": {
        "AgentName": "copilot-cli",
        "Model": "gpt-4o",
        "ResponseTimeoutSeconds": 120
      },
      "Telegram": {
        "AgentName": "copilot-cli",
        "Model": "claude-sonnet-4",
        "ResponseTimeoutSeconds": 120
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
| `Acp__CliPath` | Copilot CLI 路徑（留空則使用 SDK 內建） |
| `Acp__GithubToken` | GitHub Token（需有 Copilot 權限） |
| `Acp__ResponseTimeoutSeconds` | AI 回應逾時秒數 |
| `AgentBindings__Bindings__Line__Model` | LINE 綁定的 AI 模型 |
| `AgentBindings__Bindings__Telegram__Model` | Telegram 綁定的 AI 模型 |

## 🔌 Webhook 端點

| 平台 | 端點 | 說明 |
|------|------|------|
| LINE | `POST /api/webhook/line` | LINE Messaging API Webhook |
| Telegram | `POST /api/webhook/telegram` | Telegram Bot API Webhook |
| 健康檢查 | `GET /health` | 服務健康狀態 |

## 📂 專案結構

```
src/AiImConnector/
├── Models/           # 資料模型（UnifiedMessage、MediaContent）
├── Services/         # 核心服務
│   ├── Acp/          # Copilot SDK 封裝（CopilotClientService、CopilotSessionManager）
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
