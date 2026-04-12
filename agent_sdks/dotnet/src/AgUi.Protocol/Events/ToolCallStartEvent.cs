using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>TOOL_CALL_START — opens a new tool call invocation.</summary>
public sealed record ToolCallStartEvent : BaseEvent
{
    /// <summary>Unique identifier for this tool call.</summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; init; }

    /// <summary>Name of the tool being invoked.</summary>
    [JsonPropertyName("toolCallName")]
    public required string ToolCallName { get; init; }

    /// <summary>Optional parent message that triggered this tool call.</summary>
    [JsonPropertyName("parentMessageId")]
    public string? ParentMessageId { get; init; }
}
