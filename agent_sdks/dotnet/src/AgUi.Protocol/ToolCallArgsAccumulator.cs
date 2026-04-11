using System.Diagnostics;
using System.Text;
using AgUi.Protocol.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgUi.Protocol;

/// <summary>
/// Accumulates TOOL_CALL_ARGS delta strings per toolCallId.
/// Call <see cref="Complete"/> after TOOL_CALL_END to get final JSON.
/// </summary>
/// <remarks>
/// Not thread-safe. The internal buffer dictionary is unguarded and must be
/// mutated from a single writer. In-repo, the sole owner is
/// <c>AgentEventBridge.ProcessLoopAsync</c>, which drains the event channel on
/// one background task. Do not share an instance across threads without
/// external synchronization.
/// </remarks>
public sealed class ToolCallArgsAccumulator
{
    private readonly Dictionary<string, StringBuilder> _buffers = new();
    private readonly ILogger _logger;

    public ToolCallArgsAccumulator(ILogger<ToolCallArgsAccumulator>? logger = null)
    {
        _logger = logger ?? NullLogger<ToolCallArgsAccumulator>.Instance;
    }

    public void OnArgs(ToolCallArgsEvent args)
    {
        if (!_buffers.TryGetValue(args.ToolCallId, out var sb))
        {
            ToolCallArgsAccumulatorLog.AccumulateStarted(_logger, args.ToolCallId);
            sb = new StringBuilder();
            _buffers[args.ToolCallId] = sb;
        }
        sb.Append(args.Delta);
        ToolCallArgsAccumulatorLog.AccumulateProgress(_logger, args.ToolCallId, args.Delta.Length, sb.Length);
    }

    /// <summary>Returns the complete JSON string and removes from buffer.</summary>
    public string Complete(string toolCallId)
    {
        using var activity = Diagnostics.Source.StartActivity("ToolCallArgs.Complete", ActivityKind.Internal);
        activity?.SetTag("a2ui.tool_call_id", toolCallId);

        if (_buffers.Remove(toolCallId, out var sb))
        {
            var result = sb.ToString();
            activity?.SetTag("a2ui.total_length", result.Length);
            ToolCallArgsAccumulatorLog.AccumulateCompleted(_logger, toolCallId, result.Length);
            return result;
        }

        activity?.SetTag("a2ui.total_length", 0);
        return string.Empty;
    }

    public bool HasPending(string toolCallId) => _buffers.ContainsKey(toolCallId);

    public void Clear()
    {
        foreach (var kvp in _buffers)
        {
            ToolCallArgsAccumulatorLog.AccumulateOrphaned(_logger, kvp.Key, kvp.Value.Length);
        }
        _buffers.Clear();
    }
}

internal static partial class ToolCallArgsAccumulatorLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Tool-call args accumulation started: toolCallId={ToolCallId}"
    )]
    public static partial void AccumulateStarted(ILogger logger, string toolCallId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Trace,
        Message = "Tool-call args delta appended: toolCallId={ToolCallId}, deltaLength={DeltaLength}, totalLength={TotalLength}"
    )]
    public static partial void AccumulateProgress(ILogger logger, string toolCallId, int deltaLength, int totalLength);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Tool-call args accumulation completed: toolCallId={ToolCallId}, totalLength={TotalLength}"
    )]
    public static partial void AccumulateCompleted(ILogger logger, string toolCallId, int totalLength);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Tool-call args orphaned (Complete never called): toolCallId={ToolCallId}, totalLength={TotalLength}"
    )]
    public static partial void AccumulateOrphaned(ILogger logger, string toolCallId, int totalLength);
}
