using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>TEXT_MESSAGE_CONTENT — carries a non-empty text chunk.</summary>
public sealed record TextMessageContentEvent : BaseEvent
{
    /// <summary>Identifier of the message this chunk belongs to.</summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    /// <summary>Non-empty text chunk. Concatenate all chunks for a messageId.</summary>
    [JsonPropertyName("delta")]
    public required string Delta { get; init; }
}
