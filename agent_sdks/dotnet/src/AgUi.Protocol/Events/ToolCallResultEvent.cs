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

    /// <summary>
    /// Tool call result content.
    /// </summary>
    /// <remarks>
    /// Typed as <see cref="JsonElement"/> (broader than the <c>string</c> declared by the
    /// AG-UI reference spec in <c>docs/sdk/js/core/events.mdx</c>) to accommodate agents
    /// that return structured JSON payloads rather than a pre-flattened string. Callers
    /// expecting a plain string should use <c>Content.GetString()</c> and handle the
    /// <see cref="JsonException"/> that will surface when an agent emits a structured
    /// object. For pass-through, use <c>Content.GetRawText()</c>. A future revision MAY
    /// narrow this to <c>string</c> if the AG-UI spec's stricter declaration is
    /// enforced upstream.
    /// </remarks>
    [JsonPropertyName("content")]
    public required JsonElement Content { get; init; }

    /// <summary>Optional role for the result message.</summary>
    [JsonPropertyName("role")]
    public string? Role { get; init; }
}
