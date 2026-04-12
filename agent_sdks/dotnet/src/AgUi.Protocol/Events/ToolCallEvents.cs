using System.Text.Json;
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

/// <summary>
/// Carries raw JSON string fragments. Concatenate all deltas for a toolCallId
/// to obtain the complete JSON arguments object.
/// </summary>
public sealed record ToolCallArgsEvent : BaseEvent
{
    /// <summary>Identifier of the tool call receiving this argument fragment.</summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; init; }

    /// <summary>Raw JSON string fragment to append to the tool call arguments.</summary>
    [JsonPropertyName("delta")]
    public required string Delta { get; init; }
}

/// <summary>TOOL_CALL_END — closes a tool call invocation.</summary>
public sealed record ToolCallEndEvent : BaseEvent
{
    /// <summary>Identifier of the tool call being closed.</summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; init; }
}

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
