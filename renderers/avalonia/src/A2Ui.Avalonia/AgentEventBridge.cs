using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using A2Ui.Core;
using A2Ui.Core.Messages;
using AgUi.Protocol;
using AgUi.Protocol.Events;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2Ui.Avalonia;

/// <summary>
/// In-process bridge between an AG-UI agent and the A2UI SurfaceManager.
/// Uses System.Threading.Channels for zero-serialization event passing.
/// Only processes tool calls named "render_ui" or "update_surface" as A2UI messages.
/// </summary>
public sealed class AgentEventBridge : IDisposable
{
    private static readonly HashSet<string> s_a2uiToolNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "render_ui",
        "update_surface",
    };

    private readonly Channel<BaseEvent> _channel;
    private readonly SurfaceManager _surfaceManager;
    private readonly ILogger<AgentEventBridge> _logger;
    private readonly ToolCallArgsAccumulator _accumulator = new();
    private readonly Dictionary<string, string> _toolNames = new();
    private readonly int _capacity;
    private CancellationTokenSource? _cts;

    public AgentEventBridge(SurfaceManager surfaceManager, ILoggerFactory? loggerFactory = null, int capacity = 1024)
    {
        _surfaceManager = surfaceManager;
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<AgentEventBridge>();
        _capacity = capacity;
        _channel = Channel.CreateBounded<BaseEvent>(
            new BoundedChannelOptions(capacity) { FullMode = BoundedChannelFullMode.Wait }
        );
    }

    /// <summary>Write an event from the agent (call from agent thread).</summary>
    public ValueTask WriteEventAsync(BaseEvent evt, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(evt, ct);

    /// <summary>Start processing events on a background task.</summary>
    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ProcessLoopAsync(_cts.Token));
    }

    public void Stop() => _cts?.Cancel();

    public void Dispose() => Stop();

    // Events surfaced to the app layer
    public event EventHandler<string>? AgentTextDelta;
    public event EventHandler? RunStarted;
    public event EventHandler? RunFinished;
    public event EventHandler<string>? RunError;

    /// <summary>
    /// Fired when the user interacts with a rendered A2UI component (e.g., button click).
    /// The app must subscribe to this and serialize the action as a
    /// <see cref="ClientToServerMessage"/> to send back to the agent.
    /// <para>
    /// Wire this by connecting A2UiSurface.UserActionFired to <see cref="OnUserAction"/>:
    /// <code>surface.UserActionFired += bridge.OnUserAction;</code>
    /// </para>
    /// </summary>
    public event EventHandler<UserActionEventArgs>? UserActionReceived;

    /// <summary>
    /// Event handler to connect to <see cref="A2Ui.Avalonia.Controls.A2UiSurface.UserActionFired"/>.
    /// Forwards the action to <see cref="UserActionReceived"/> subscribers.
    /// </summary>
    public void OnUserAction(object? sender, UserActionEventArgs e)
    {
        try
        {
            UserActionReceived?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            BridgeLog.UserActionSubscriberThrew(_logger, ex);
        }
    }

    private async Task ProcessLoopAsync(CancellationToken ct)
    {
        BridgeLog.ProcessLoopStarted(_logger, _capacity);
        var stopwatch = Stopwatch.StartNew();
        int eventCount = 0;

        using var activity = Diagnostics.BridgeSource.StartActivity("EventBridge.ProcessLoop", ActivityKind.Consumer);

        try
        {
            await foreach (var evt in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                eventCount++;
                string eventType = evt.GetType().Name;
                BridgeLog.EventDispatched(_logger, eventType, eventCount);

                using var dispatchActivity = Diagnostics.BridgeSource.StartActivity(
                    "EventBridge.DispatchEvent",
                    ActivityKind.Internal
                );
                dispatchActivity?.SetTag("a2ui.event_type", eventType);
                dispatchActivity?.SetTag("a2ui.event_index", eventCount);

                switch (evt)
                {
                    case RunStartedEvent:
                        PostSafe(() => RunStarted?.Invoke(this, EventArgs.Empty));
                        break;

                    case RunFinishedEvent:
                        PostSafe(() => RunFinished?.Invoke(this, EventArgs.Empty));
                        break;

                    case RunErrorEvent err:
                        PostSafe(() => RunError?.Invoke(this, err.Message));
                        break;

                    case TextMessageContentEvent tc:
                        PostSafe(() => AgentTextDelta?.Invoke(this, tc.Delta));
                        break;

                    case ToolCallStartEvent start:
                        _toolNames[start.ToolCallId] = start.ToolCallName;
                        break;

                    case ToolCallArgsEvent args:
                        _accumulator.OnArgs(args);
                        break;

                    case ToolCallEndEvent end:
                        string json = _accumulator.Complete(end.ToolCallId);
                        bool isA2Ui =
                            _toolNames.TryGetValue(end.ToolCallId, out var toolName)
                            && s_a2uiToolNames.Contains(toolName);
                        _toolNames.Remove(end.ToolCallId);
                        if (isA2Ui && !string.IsNullOrWhiteSpace(json))
                            ProcessA2UiPayload(json, end.ToolCallId);
                        break;
                }
            }

            stopwatch.Stop();
            BridgeLog.ProcessLoopCompleted(_logger, eventCount, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown via Stop()/Dispose() — log at Information as completed.
            stopwatch.Stop();
            BridgeLog.ProcessLoopCompleted(_logger, eventCount, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            BridgeLog.ProcessLoopFailed(_logger, eventCount, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
    }

    /// <summary>
    /// Post an action to the UI thread, catching any exceptions to prevent
    /// unhandled exceptions from crashing the Avalonia dispatcher thread.
    /// </summary>
    private void PostSafe(Action action)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                BridgeLog.UiThreadActionFailed(_logger, ex);
            }
        });
    }

    private void ProcessA2UiPayload(string json, string toolCallId)
    {
        // A2UI payload is JSONL — one A2UiMessage per line
        int messageCount = 0;
        foreach (var line in json.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var msg = JsonSerializer.Deserialize<A2UiMessage>(line.Trim());
                if (msg is not null)
                {
                    // Validate on background thread — skip invalid messages
                    // SurfaceManager.Process() also validates defensively,
                    // but catching here prevents posting invalid work to UI thread
                    msg.Validate();
                    PostSafe(() => _surfaceManager.Process(msg));
                    messageCount++;
                }
            }
            catch (JsonException ex)
            {
                BridgeLog.MalformedA2UiLine(_logger, line[..Math.Min(line.Length, 200)], ex);
            }
            catch (A2UiMessageValidationException ex)
            {
                BridgeLog.InvalidA2UiMessage(_logger, line[..Math.Min(line.Length, 200)], ex);
            }
        }
        BridgeLog.A2UiMessageReceived(_logger, toolCallId, messageCount);
    }
}

internal static partial class BridgeLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "UserActionReceived subscriber threw")]
    public static partial void UserActionSubscriberThrew(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Skipping malformed A2UI line: {LinePreview}")]
    public static partial void MalformedA2UiLine(ILogger logger, string linePreview, Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Skipping invalid A2UI message: {LinePreview}")]
    public static partial void InvalidA2UiMessage(ILogger logger, string linePreview, Exception exception);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Exception in UI thread action dispatched by AgentEventBridge"
    )]
    public static partial void UiThreadActionFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Event processing loop started (channel capacity={ChannelCapacity})"
    )]
    public static partial void ProcessLoopStarted(ILogger logger, int channelCapacity);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "Event processing loop completed: eventCount={EventCount}, durationMs={DurationMs}"
    )]
    public static partial void ProcessLoopCompleted(ILogger logger, int eventCount, long durationMs);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Error,
        Message = "Event processing loop failed after {EventCount} events in {DurationMs}ms"
    )]
    public static partial void ProcessLoopFailed(ILogger logger, int eventCount, long durationMs, Exception exception);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Debug,
        Message = "AG-UI event dispatched: eventType={EventType}, eventIndex={EventIndex}"
    )]
    public static partial void EventDispatched(ILogger logger, string eventType, int eventIndex);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Debug,
        Message = "A2UI message received from tool call: toolCallId={ToolCallId}, messageCount={MessageCount}"
    )]
    public static partial void A2UiMessageReceived(ILogger logger, string toolCallId, int messageCount);
}
