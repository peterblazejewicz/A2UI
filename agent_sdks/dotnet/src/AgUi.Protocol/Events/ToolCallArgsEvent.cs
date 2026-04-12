using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

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
