# Copilot Instructions — AI IM Connector

## Build, Test, and Lint

```bash
# Restore, build, test (from repo root)
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --verbosity normal

# Run a single test by fully-qualified name
dotnet test --filter "FullyQualifiedName~AiImConnector.Tests.Services.MediaHandlerTests.DetectMediaType_應正確判斷多媒體類型"

# Run all tests in one class
dotnet test --filter "FullyQualifiedName~AiImConnector.Tests.Services.AiResponseParserTests"

# Run from the main project directory
cd src/ImConnector && dotnet run
```

CI runs `dotnet build --no-restore --configuration Release` then `dotnet test --no-build --configuration Release`.

## Architecture

This is a .NET 8 WebAPI that bridges IM platforms (LINE, Telegram) to AI agents via the GitHub Copilot SDK (ACP protocol). The message flow is:

```
IM Platform → Webhook Controller → MessageRouter → CopilotSessionManager → Copilot CLI (ACP)
                                        ↓
                               AiResponseParser (extract media from AI text)
                                        ↓
                               MediaHostingService (base64 → temp URL)
                                        ↓
                               IM Adapter → Reply to user
```

### Key services and their responsibilities

- **`CopilotSessionManager`** (Singleton) — Manages per-user `CopilotSession` instances with thread-safe creation (per-user `SemaphoreSlim`), stale session auto-rebuild, and first-message System Prompt injection.
- **`MessageRouter`** (Singleton) — Orchestrates the full message lifecycle: command handling (`/clear`, `/help`, `/status`), prompt assembly (text + media descriptions), AI communication, response parsing, and media URL attachment.
- **`AiResponseParser`** (static) — Parses AI text responses to extract Markdown images, Base64 Data URIs, standalone image URLs, and detects local file paths. All regexes have 1-second timeouts for ReDoS protection.
- **`MediaHostingService`** (Singleton) — In-memory Base64 media store with 10-minute TTL, auto-cleanup timer, 1000-entry/500MB capacity limits, and cryptographically random IDs.
- **`CopilotLifecycleService`** — `BackgroundService` that starts/stops the Copilot CLI with the application.

### Adapter pattern

Each IM platform has three files under `Adapters/{Platform}/`:

| File | Role |
|------|------|
| `{Platform}Adapter.cs` | Implements `IImAdapter` (ReplyTextAsync, ReplyMediaAsync) |
| `{Platform}WebhookController.cs` | HTTP POST endpoint for receiving webhooks |
| `{Platform}MessageConverter.cs` | Converts platform messages ↔ `UnifiedMessage` |

A template with step-by-step instructions exists at `Adapters/_Template/README.md`. When adding a new platform, you also need to add settings in `Configuration/ImSettings.cs`, register DI in `Program.cs`, and add agent binding config in `appsettings.json`.

### Configuration structure

Settings bind to these `appsettings.json` sections via `IOptions<T>`:

| Class | Section | Purpose |
|-------|---------|---------|
| `ConnectorSettings` | `Connector` | Public base URL for media hosting |
| `LineSettings` | `Im:Line` | Channel token and secret |
| `TelegramSettings` | `Im:Telegram` | Bot token and webhook secret |
| `AcpSettings` | `Acp` | Copilot CLI URL and response timeout |
| `AgentBindingSettings` | `AgentBindings` | Per-platform agent name, model, timeout, system prompt |

Secrets are managed via `dotnet user-secrets` in development and environment variables in Docker.

## Conventions

- **Language**: Code comments, XML docs, log messages, and commit messages are in Traditional Chinese (繁體中文). Test method names use Chinese descriptive names (e.g., `DetectMediaType_應正確判斷多媒體類型`).
- **Namespace**: Root namespace is `AiImConnector`, mirroring the folder structure (e.g., `AiImConnector.Adapters.Line`, `AiImConnector.Services.Acp`).
- **DI lifetime**: Core services (`CopilotSessionManager`, `MessageRouter`, `MediaHostingService`) are Singleton because they hold in-memory state (session caches, media store). `IMediaHandler` uses `AddHttpClient<>` for managed `HttpClient`.
- **Thread safety**: Session creation uses per-user `SemaphoreSlim` with double-check locking. System Prompt injection uses `ConcurrentDictionary.TryAdd` as an atomic check-and-set.
- **Security patterns**: Webhook signature validation uses `CryptographicOperations.FixedTimeEquals` (constant-time comparison). Media IDs use `RandomNumberGenerator`. Request size limits are set on webhook endpoints. All user-facing error messages hide internal details in Production.
- **Testing**: xUnit + Moq. Tests live under `tests/AiImConnector.Tests/` mirroring the `src/` folder structure (`Adapters/`, `Services/`). Use `NullLogger<T>` for logger dependencies.
- **Webhook controllers** fire-and-forget (`Task.Run`) for message processing to avoid blocking the webhook response to the IM platform.
