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
    public async Task ProcessLoop_NonA2UiToolCall_IgnoredAsync()
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
    public async Task ProcessLoop_RunStartedAndFinished_FiresEventsAsync()
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
    public async Task ProcessLoop_RunError_FiresErrorEventAsync()
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
    public async Task ProcessLoop_TextMessageContent_FiresAgentTextDeltaAsync()
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

    // ── End-to-end tool call → surface processing tests ──

    [AvaloniaFact]
    public async Task ProcessLoop_ValidRenderUiToolCall_CreatesSurfaceAsync()
    {
        var sm = new SurfaceManager();
        using var bridge = new AgentEventBridge(sm);

        bool surfaceCreated = false;
        sm.SurfaceCreated += (_, _) => surfaceCreated = true;

        bridge.Start();

        // Simulate a complete render_ui tool call with a createSurface JSONL payload
        await bridge
            .WriteEventAsync(new ToolCallStartEvent { ToolCallId = "tc-e2e", ToolCallName = "render_ui" })
            .ConfigureAwait(true);
        await bridge
            .WriteEventAsync(
                new ToolCallArgsEvent
                {
                    ToolCallId = "tc-e2e",
                    Delta = """{"version":"v0.9","createSurface":{"surfaceId":"e2e-test","catalogId":"basic"}}""",
                }
            )
            .ConfigureAwait(true);
        await bridge.WriteEventAsync(new ToolCallEndEvent { ToolCallId = "tc-e2e" }).ConfigureAwait(true);

        await DrainAndPumpAsync().ConfigureAwait(true);
        bridge.Stop();

        surfaceCreated.Should().BeTrue("the bridge should process valid render_ui tool calls");
        sm.GetSurface("e2e-test").Should().NotBeNull();
        sm.GetSurface("e2e-test")!.CatalogId.Should().Be("basic");
    }

    [AvaloniaFact]
    public async Task ProcessLoop_ValidRenderUiToolCall_ProcessesUpdateComponentsAsync()
    {
        var sm = new SurfaceManager();
        using var bridge = new AgentEventBridge(sm);

        bridge.Start();

        // First tool call: createSurface
        await bridge
            .WriteEventAsync(new ToolCallStartEvent { ToolCallId = "tc-create", ToolCallName = "render_ui" })
            .ConfigureAwait(true);
        await bridge
            .WriteEventAsync(
                new ToolCallArgsEvent
                {
                    ToolCallId = "tc-create",
                    Delta = """{"version":"v0.9","createSurface":{"surfaceId":"e2e-uc","catalogId":"basic"}}""",
                }
            )
            .ConfigureAwait(true);
        await bridge.WriteEventAsync(new ToolCallEndEvent { ToolCallId = "tc-create" }).ConfigureAwait(true);

        await DrainAndPumpAsync().ConfigureAwait(true);

        // Second tool call: updateComponents (multi-line JSONL)
        const string UpdateJsonl =
            """{"version":"v0.9","updateComponents":{"surfaceId":"e2e-uc","components":[{"id":"root","component":"Column"},{"id":"t1","component":"Text"}]}}""";

        await bridge
            .WriteEventAsync(new ToolCallStartEvent { ToolCallId = "tc-update", ToolCallName = "render_ui" })
            .ConfigureAwait(true);
        await bridge
            .WriteEventAsync(new ToolCallArgsEvent { ToolCallId = "tc-update", Delta = UpdateJsonl })
            .ConfigureAwait(true);
        await bridge.WriteEventAsync(new ToolCallEndEvent { ToolCallId = "tc-update" }).ConfigureAwait(true);

        await DrainAndPumpAsync().ConfigureAwait(true);
        bridge.Stop();

        var surface = sm.GetSurface("e2e-uc");
        surface.Should().NotBeNull();
        surface!.Components.Should().HaveCount(2);
        surface.Components.Should().ContainKey("root");
        surface.Components.Should().ContainKey("t1");
    }

    [AvaloniaFact]
    public async Task ProcessLoop_InvalidToolCallPayload_SkipsAndContinuesAsync()
    {
        var sm = new SurfaceManager();
        using var bridge = new AgentEventBridge(sm);

        bridge.Start();

        // First tool call: invalid JSON payload
        await bridge
            .WriteEventAsync(new ToolCallStartEvent { ToolCallId = "tc-bad", ToolCallName = "render_ui" })
            .ConfigureAwait(true);
        await bridge
            .WriteEventAsync(new ToolCallArgsEvent { ToolCallId = "tc-bad", Delta = "not valid json {{{" })
            .ConfigureAwait(true);
        await bridge.WriteEventAsync(new ToolCallEndEvent { ToolCallId = "tc-bad" }).ConfigureAwait(true);

        await DrainAndPumpAsync().ConfigureAwait(true);

        // Second tool call: valid payload — bridge should have recovered
        await bridge
            .WriteEventAsync(new ToolCallStartEvent { ToolCallId = "tc-good", ToolCallName = "render_ui" })
            .ConfigureAwait(true);
        await bridge
            .WriteEventAsync(
                new ToolCallArgsEvent
                {
                    ToolCallId = "tc-good",
                    Delta = """{"version":"v0.9","createSurface":{"surfaceId":"recovered","catalogId":"basic"}}""",
                }
            )
            .ConfigureAwait(true);
        await bridge.WriteEventAsync(new ToolCallEndEvent { ToolCallId = "tc-good" }).ConfigureAwait(true);

        await DrainAndPumpAsync().ConfigureAwait(true);
        bridge.Stop();

        // The invalid payload should have been skipped; the valid one should succeed
        sm.GetSurface("recovered").Should().NotBeNull("bridge should recover from invalid payloads");
    }
}
