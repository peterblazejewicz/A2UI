using A2Ui.Core;
using AgUi.Protocol.Events;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests;

public sealed class AgentEventBridgeTests
{
    /// <summary>
    /// After writing events and before asserting, give the background processing
    /// loop time to consume from the channel and post to the dispatcher, then
    /// pump the dispatcher so the posted lambdas execute on the test thread.
    /// </summary>
    private static async Task DrainAndPumpAsync()
    {
        // Allow background task to read from channel and call Dispatcher.Post
        await Task.Delay(100).ConfigureAwait(true);
        // Execute all posted dispatcher jobs on the current (UI) thread
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void OnUserAction_ForwardsToUserActionReceived()
    {
        var sm = new SurfaceManager();
        using var bridge = new AgentEventBridge(sm);

        UserActionEventArgs? received = null;
        bridge.UserActionReceived += (_, e) => received = e;

        var args = new UserActionEventArgs("surface-1", "click", "payload", "btn-1");
        bridge.OnUserAction(this, args);

        received.Should().NotBeNull();
        received!.SurfaceId.Should().Be("surface-1");
        received.EventName.Should().Be("click");
        received.ComponentId.Should().Be("btn-1");
    }

    [AvaloniaFact]
    public void OnUserAction_SubscriberThrows_DoesNotPropagate()
    {
        var sm = new SurfaceManager();
        using var bridge = new AgentEventBridge(sm);

        bridge.UserActionReceived += (_, _) => throw new InvalidOperationException("boom");

        var args = new UserActionEventArgs("surface-1", "click", null);

        // Should not throw despite subscriber throwing
        var act = () => bridge.OnUserAction(this, args);
        act.Should().NotThrow();
    }

    [AvaloniaFact]
    public async Task ProcessLoop_NonA2UiToolCall_Ignored()
    {
        var sm = new SurfaceManager();
        using var bridge = new AgentEventBridge(sm);
        bool surfaceCreated = false;
        sm.SurfaceCreated += (_, _) => surfaceCreated = true;

        bridge.Start();

        // Tool call with a non-A2UI name ("search") should be ignored
        await bridge
            .WriteEventAsync(new ToolCallStartEvent { ToolCallId = "tc-1", ToolCallName = "search" })
            .ConfigureAwait(true);
        await bridge
            .WriteEventAsync(new ToolCallArgsEvent { ToolCallId = "tc-1", Delta = """{"query":"hello"}""" })
            .ConfigureAwait(true);
        await bridge.WriteEventAsync(new ToolCallEndEvent { ToolCallId = "tc-1" }).ConfigureAwait(true);

        await DrainAndPumpAsync().ConfigureAwait(true);
        bridge.Stop();

        surfaceCreated.Should().BeFalse();
    }

    [AvaloniaFact]
    public async Task ProcessLoop_RunStartedAndFinished_FiresEvents()
    {
        var sm = new SurfaceManager();
        using var bridge = new AgentEventBridge(sm);

        bool started = false;
        bool finished = false;
        bridge.RunStarted += (_, _) => started = true;
        bridge.RunFinished += (_, _) => finished = true;

        bridge.Start();

        await bridge.WriteEventAsync(new RunStartedEvent { ThreadId = "t-1", RunId = "r-1" }).ConfigureAwait(true);
        await bridge.WriteEventAsync(new RunFinishedEvent { ThreadId = "t-1", RunId = "r-1" }).ConfigureAwait(true);

        await DrainAndPumpAsync().ConfigureAwait(true);
        bridge.Stop();

        started.Should().BeTrue();
        finished.Should().BeTrue();
    }

    [AvaloniaFact]
    public async Task ProcessLoop_RunError_FiresErrorEvent()
    {
        var sm = new SurfaceManager();
        using var bridge = new AgentEventBridge(sm);

        string? errorMessage = null;
        bridge.RunError += (_, msg) => errorMessage = msg;

        bridge.Start();

        await bridge.WriteEventAsync(new RunErrorEvent { Message = "something went wrong" }).ConfigureAwait(true);

        await DrainAndPumpAsync().ConfigureAwait(true);
        bridge.Stop();

        errorMessage.Should().Be("something went wrong");
    }

    [AvaloniaFact]
    public async Task ProcessLoop_TextMessageContent_FiresAgentTextDelta()
    {
        var sm = new SurfaceManager();
        using var bridge = new AgentEventBridge(sm);

        string? delta = null;
        bridge.AgentTextDelta += (_, d) => delta = d;

        bridge.Start();

        await bridge
            .WriteEventAsync(new TextMessageContentEvent { MessageId = "msg-1", Delta = "Hello, world!" })
            .ConfigureAwait(true);

        await DrainAndPumpAsync().ConfigureAwait(true);
        bridge.Stop();

        delta.Should().Be("Hello, world!");
    }
}
