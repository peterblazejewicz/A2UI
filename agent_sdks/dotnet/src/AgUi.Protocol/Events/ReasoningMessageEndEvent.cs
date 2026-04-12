using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>REASONING_MESSAGE_END — closes a reasoning message stream.</summary>
public sealed record ReasoningMessageEndEvent : BaseEvent
{
    /// <summary>Identifier of the reasoning message being closed.</summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }
}
