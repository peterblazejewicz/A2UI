using System.Text.Json;
using System.Threading.Channels;
using A2Ui.Core;
using A2Ui.Core.Messages;
using AgUi.Protocol;
using AgUi.Protocol.Events;
using Avalonia.Threading;

namespace A2Ui.Rendering;

/// <summary>
/// In-process bridge between an AG-UI agent and the A2UI SurfaceManager.
/// Uses System.Threading.Channels for zero-serialization event passing.
/// Processes tool calls named "render_ui" or "update_surface" as A2UI messages.
/// </summary>
public sealed class AgentEventBridge : IDisposable
{
    private readonly Channel<BaseEvent>   _channel;
    private readonly SurfaceManager      _surfaceManager;
    private readonly ToolCallArgsAccumulator _accumulator = new();
    private CancellationTokenSource?     _cts;

    public AgentEventBridge(SurfaceManager surfaceManager, int capacity = 1024)
    {
        _surfaceManager = surfaceManager;
        _channel = Channel.CreateBounded<BaseEvent>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
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
    public event EventHandler?         RunStarted;
    public event EventHandler?         RunFinished;
    public event EventHandler<string>? RunError;

    private async Task ProcessLoopAsync(CancellationToken ct)
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync(ct))
        {
            switch (evt)
            {
                case RunStartedEvent:
                    Dispatcher.UIThread.Post(() => RunStarted?.Invoke(this, EventArgs.Empty));
                    break;

                case RunFinishedEvent:
                    Dispatcher.UIThread.Post(() => RunFinished?.Invoke(this, EventArgs.Empty));
                    break;

                case RunErrorEvent err:
                    Dispatcher.UIThread.Post(() => RunError?.Invoke(this, err.Message));
                    break;

                case TextMessageContentEvent tc:
                    Dispatcher.UIThread.Post(() => AgentTextDelta?.Invoke(this, tc.Delta));
                    break;

                case ToolCallArgsEvent args:
                    _accumulator.OnArgs(args);
                    break;

                case ToolCallEndEvent end:
                    string json = _accumulator.Complete(end.ToolCallId);
                    if (!string.IsNullOrWhiteSpace(json))
                        ProcessA2UiPayload(json);
                    break;
            }
        }
    }

    private void ProcessA2UiPayload(string json)
    {
        // A2UI payload is JSONL — one A2UiMessage per line
        foreach (var line in json.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var msg = JsonSerializer.Deserialize<A2UiMessage>(line.Trim());
                if (msg is not null)
                    Dispatcher.UIThread.Post(() => _surfaceManager.Process(msg));
            }
            catch (JsonException)
            {
                // Partial/malformed line — skip
            }
        }
    }
}