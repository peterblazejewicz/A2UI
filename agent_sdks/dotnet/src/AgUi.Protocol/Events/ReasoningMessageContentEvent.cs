using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>REASONING_MESSAGE_CONTENT — carries a non-empty reasoning text chunk.</summary>
public sealed record ReasoningMessageContentEvent : BaseEvent
{
    /// <summary>Identifier of the reasoning message this chunk belongs to.</summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    /// <summary>Non-empty reasoning text chunk.</summary>
    [JsonPropertyName("delta")]
    public required string Delta { get; init; }
}
