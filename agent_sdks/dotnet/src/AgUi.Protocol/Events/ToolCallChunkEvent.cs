using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>Convenience: auto-expands to Start→Args→End.</summary>
public sealed record ToolCallChunkEvent : BaseEvent
{
    /// <summary>Tool call identifier; required on the first chunk.</summary>
    [JsonPropertyName("toolCallId")]
    public string? ToolCallId { get; init; }

    /// <summary>Tool name; required on the first chunk.</summary>
    [JsonPropertyName("toolCallName")]
    public string? ToolCallName { get; init; }

    /// <summary>Optional parent message that triggered this tool call.</summary>
    [JsonPropertyName("parentMessageId")]
    public string? ParentMessageId { get; init; }

    /// <summary>JSON argument fragment to append.</summary>
    [JsonPropertyName("delta")]
    public string? Delta { get; init; }
}
