using AiImConnector.Services.Acp;

namespace AiImConnector.Tests.Services;

/// <summary>
/// CopilotSessionManager 單元測試（僅測試不需要啟動 CLI 的邏輯）
/// </summary>
public class SessionServiceTests
{
    [Fact]
    public void BuildSessionId_應正確產生SessionId()
    {
        var id = CopilotSessionManager.BuildSessionId("Line", "user001");
        Assert.Equal("Line:user001", id);
    }

    [Fact]
    public void BuildSessionId_Telegram平台()
    {
        var id = CopilotSessionManager.BuildSessionId("Telegram", "12345");
        Assert.Equal("Telegram:12345", id);
    }

    [Fact]
    public void BuildSessionId_不同使用者應產生不同Id()
    {
        var id1 = CopilotSessionManager.BuildSessionId("Line", "user001");
        var id2 = CopilotSessionManager.BuildSessionId("Line", "user002");
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void BuildSessionId_不同平台應產生不同Id()
    {
        var id1 = CopilotSessionManager.BuildSessionId("Line", "user001");
        var id2 = CopilotSessionManager.BuildSessionId("Telegram", "user001");
        Assert.NotEqual(id1, id2);
    }
}
