using AiImConnector.Models;

namespace AiImConnector.Tests.Services;

/// <summary>
/// UnifiedMessage 模型單元測試
/// </summary>
public class UnifiedMessageTests
{
    [Fact]
    public void IsCommand_以斜線開頭_應為true()
    {
        var msg = new UnifiedMessage { Text = "/clear" };
        Assert.True(msg.IsCommand);
    }

    [Fact]
    public void IsCommand_一般文字_應為false()
    {
        var msg = new UnifiedMessage { Text = "你好" };
        Assert.False(msg.IsCommand);
    }

    [Fact]
    public void IsCommand_null文字_應為false()
    {
        var msg = new UnifiedMessage { Text = null };
        Assert.False(msg.IsCommand);
    }

    [Fact]
    public void CommandName_應正確解析指令名稱()
    {
        var msg = new UnifiedMessage { Text = "/status" };
        Assert.Equal("status", msg.CommandName);
    }

    [Fact]
    public void CommandArgs_應正確解析指令參數()
    {
        var msg = new UnifiedMessage { Text = "/switch copilot-cli" };
        Assert.Equal("copilot-cli", msg.CommandArgs);
    }

    [Fact]
    public void CommandArgs_無參數_應為空字串()
    {
        var msg = new UnifiedMessage { Text = "/help" };
        Assert.Equal("", msg.CommandArgs);
    }
}
