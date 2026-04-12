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

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallArgsAccumulator"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for accumulation diagnostics.</param>
    public ToolCallArgsAccumulator(ILogger<ToolCallArgsAccumulator>? logger = null)
    {
        _logger = logger ?? NullLogger<ToolCallArgsAccumulator>.Instance;
    }

    /// <summary>Appends a TOOL_CALL_ARGS delta to the buffer for its tool call.</summary>
    /// <param name="args">The tool call args event containing the delta fragment.</param>
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

    /// <summary>Returns whether there are buffered argument fragments for the given tool call.</summary>
    /// <param name="toolCallId">The tool call identifier to check.</param>
    /// <returns><see langword="true"/> if fragments are pending for the tool call.</returns>
    public bool HasPending(string toolCallId) => _buffers.ContainsKey(toolCallId);

    /// <summary>Clears all pending buffers, logging any orphaned (never-completed) entries.</summary>
    public void Clear()
    {
        foreach (var kvp in _buffers)
        {
            ToolCallArgsAccumulatorLog.AccumulateOrphaned(_logger, kvp.Key, kvp.Value.Length);
        }
        _buffers.Clear();
    }
}
