using System.Text;
using AgUi.Protocol.Events;

namespace AgUi.Protocol;

/// <summary>
/// Accumulates TOOL_CALL_ARGS delta strings per toolCallId.
/// Call <see cref="Complete"/> after TOOL_CALL_END to get final JSON.
/// </summary>
public sealed class ToolCallArgsAccumulator
{
    private readonly Dictionary<string, StringBuilder> _buffers = new();

    public void OnArgs(ToolCallArgsEvent args)
    {
        if (!_buffers.TryGetValue(args.ToolCallId, out var sb))
        {
            sb = new StringBuilder();
            _buffers[args.ToolCallId] = sb;
        }
        sb.Append(args.Delta);
    }

    /// <summary>Returns the complete JSON string and removes from buffer.</summary>
    public string Complete(string toolCallId)
    {
        if (_buffers.Remove(toolCallId, out var sb))
            return sb.ToString();
        return string.Empty;
    }

    public bool HasPending(string toolCallId) => _buffers.ContainsKey(toolCallId);

    public void Clear() => _buffers.Clear();
}