using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>TOOL_CALL_END — closes a tool call invocation.</summary>
public sealed record ToolCallEndEvent : BaseEvent
{
    /// <summary>Identifier of the tool call being closed.</summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; init; }
}
