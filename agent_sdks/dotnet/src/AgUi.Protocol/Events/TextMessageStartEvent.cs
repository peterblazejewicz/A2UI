using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>TEXT_MESSAGE_START — opens a new text message stream.</summary>
public sealed record TextMessageStartEvent : BaseEvent
{
    /// <summary>Unique identifier for this message.</summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    /// <summary>Message role, defaults to "assistant".</summary>
    [JsonPropertyName("role")]
    public string Role { get; init; } = "assistant";
}
