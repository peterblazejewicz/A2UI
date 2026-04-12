using AgUi.Protocol;
using AgUi.Protocol.Events;

namespace AgUi.Protocol.Tests;

public sealed class ToolCallArgsAccumulatorTests
{
    [Fact]
    public void OnArgs_MultipleKeys_AccumulatesIndependently()
    {
        var acc = new ToolCallArgsAccumulator();
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "{\"a\":" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc2", Delta = "{\"b\":" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "1}" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc2", Delta = "2}" });

        Assert.Equal("{\"a\":1}", acc.Complete("tc1"));
        Assert.Equal("{\"b\":2}", acc.Complete("tc2"));
    }

    [Fact]
    public void Complete_MissingKey_ReturnsEmpty()
    {
        var acc = new ToolCallArgsAccumulator();

        Assert.Empty(acc.Complete("nonexistent"));
    }

    [Fact]
    public void Complete_CalledTwice_ReturnsEmptyOnSecond()
    {
        var acc = new ToolCallArgsAccumulator();
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "hello" });

        Assert.Equal("hello", acc.Complete("tc1"));
        Assert.Empty(acc.Complete("tc1"));
    }

    [Fact]
    public void HasPending_ExistingKey_ReturnsTrue()
    {
        var acc = new ToolCallArgsAccumulator();
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "x" });

        Assert.True(acc.HasPending("tc1"));
        Assert.False(acc.HasPending("tc2"));
    }

    [Fact]
    public void Clear_RemovesAllPending()
    {
        var acc = new ToolCallArgsAccumulator();
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "a" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc2", Delta = "b" });

        acc.Clear();

        Assert.False(acc.HasPending("tc1"));
        Assert.False(acc.HasPending("tc2"));
    }

    [Fact]
    public void OnArgs_EmptyDelta_StillAccumulates()
    {
        var acc = new ToolCallArgsAccumulator();
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "start" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "end" });

        Assert.Equal("startend", acc.Complete("tc1"));
    }
}
