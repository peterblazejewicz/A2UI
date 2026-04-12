using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>TOOL_CALL_RESULT — returns the result of a completed tool call.</summary>
public sealed record ToolCallResultEvent : BaseEvent
{
    /// <summary>Message identifier for the result.</summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    /// <summary>Identifier of the tool call this result belongs to.</summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; init; }

    /// <summary>Tool call result content as a JSON element.</summary>
    [JsonPropertyName("content")]
    public required JsonElement Content { get; init; }

    /// <summary>Optional role for the result message.</summary>
    [JsonPropertyName("role")]
    public string? Role { get; init; }
}
