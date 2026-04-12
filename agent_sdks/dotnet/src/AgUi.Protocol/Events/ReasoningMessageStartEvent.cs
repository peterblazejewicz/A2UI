using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>REASONING_MESSAGE_START — opens a reasoning message stream.</summary>
public sealed record ReasoningMessageStartEvent : BaseEvent
{
    /// <summary>Unique identifier for this reasoning message.</summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    /// <summary>Message role, defaults to "assistant".</summary>
    [JsonPropertyName("role")]
    public string Role { get; init; } = "assistant";
}
