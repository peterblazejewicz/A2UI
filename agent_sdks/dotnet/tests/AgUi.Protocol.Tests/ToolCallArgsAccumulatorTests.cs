using AgUi.Protocol;
using AgUi.Protocol.Events;
using FluentAssertions;

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

        acc.Complete("tc1").Should().Be("{\"a\":1}");
        acc.Complete("tc2").Should().Be("{\"b\":2}");
    }

    [Fact]
    public void Complete_MissingKey_ReturnsEmpty()
    {
        var acc = new ToolCallArgsAccumulator();

        acc.Complete("nonexistent").Should().BeEmpty();
    }

    [Fact]
    public void Complete_CalledTwice_ReturnsEmptyOnSecond()
    {
        var acc = new ToolCallArgsAccumulator();
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "hello" });

        acc.Complete("tc1").Should().Be("hello");
        acc.Complete("tc1").Should().BeEmpty();
    }

    [Fact]
    public void HasPending_ExistingKey_ReturnsTrue()
    {
        var acc = new ToolCallArgsAccumulator();
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "x" });

        acc.HasPending("tc1").Should().BeTrue();
        acc.HasPending("tc2").Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllPending()
    {
        var acc = new ToolCallArgsAccumulator();
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "a" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc2", Delta = "b" });

        acc.Clear();

        acc.HasPending("tc1").Should().BeFalse();
        acc.HasPending("tc2").Should().BeFalse();
    }

    [Fact]
    public void OnArgs_EmptyDelta_StillAccumulates()
    {
        var acc = new ToolCallArgsAccumulator();
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "start" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "" });
        acc.OnArgs(new ToolCallArgsEvent { ToolCallId = "tc1", Delta = "end" });

        acc.Complete("tc1").Should().Be("startend");
    }
}
