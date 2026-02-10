using AiImConnector.Models;
using AiImConnector.Services.Session;

namespace AiImConnector.Tests.Services;

/// <summary>
/// SessionService 單元測試
/// </summary>
public class SessionServiceTests
{
    private readonly SessionService _service;
    private readonly InMemorySessionStore _store;

    public SessionServiceTests()
    {
        _store = new InMemorySessionStore();
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<SessionService>();
        _service = new SessionService(_store, logger);
    }

    [Fact]
    public async Task GetOrCreateAsync_新使用者_應建立新Session()
    {
        // Arrange & Act
        var session = await _service.GetOrCreateAsync("Line", "user123");

        // Assert
        Assert.NotNull(session);
        Assert.Equal("Line:user123", session.SessionId);
        Assert.Equal("Line", session.Platform);
        Assert.Equal("user123", session.UserId);
        Assert.Empty(session.History);
    }

    [Fact]
    public async Task GetOrCreateAsync_同一使用者_應回傳同一Session()
    {
        // Arrange
        var session1 = await _service.GetOrCreateAsync("Line", "user123");

        // Act
        var session2 = await _service.GetOrCreateAsync("Line", "user123");

        // Assert
        Assert.Equal(session1.SessionId, session2.SessionId);
    }

    [Fact]
    public async Task AddEntryAsync_應新增對話紀錄()
    {
        // Arrange
        await _service.GetOrCreateAsync("Telegram", "user456");

        // Act
        await _service.AddEntryAsync("Telegram", "user456", "user", "你好");
        await _service.AddEntryAsync("Telegram", "user456", "assistant", "你好！有什麼我可以幫助你的嗎？");

        // Assert
        var session = await _service.GetOrCreateAsync("Telegram", "user456");
        Assert.Equal(2, session.History.Count);
        Assert.Equal("user", session.History[0].Role);
        Assert.Equal("你好", session.History[0].Content);
        Assert.Equal("assistant", session.History[1].Role);
    }

    [Fact]
    public async Task ClearAsync_應清除Session()
    {
        // Arrange
        await _service.GetOrCreateAsync("Line", "user789");
        await _service.AddEntryAsync("Line", "user789", "user", "測試");

        // Act
        await _service.ClearAsync("Line", "user789");

        // Assert — 清除後取得的是新的 Session
        var session = await _service.GetOrCreateAsync("Line", "user789");
        Assert.Empty(session.History);
    }

    [Fact]
    public void BuildSessionId_應正確產生SessionId()
    {
        var id = SessionService.BuildSessionId("Telegram", "user001");
        Assert.Equal("Telegram:user001", id);
    }
}
