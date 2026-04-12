using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>REASONING_MESSAGE_CHUNK — convenience event that auto-expands to Start, Content, End.</summary>
public sealed record ReasoningMessageChunkEvent : BaseEvent
{
    /// <summary>Message identifier; required on the first chunk.</summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }

    /// <summary>Reasoning text chunk to append.</summary>
    [JsonPropertyName("delta")]
    public string? Delta { get; init; }
}
