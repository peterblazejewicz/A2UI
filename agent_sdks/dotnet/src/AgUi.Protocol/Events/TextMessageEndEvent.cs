using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>TEXT_MESSAGE_END — closes a text message stream.</summary>
public sealed record TextMessageEndEvent : BaseEvent
{
    /// <summary>Identifier of the message being closed.</summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }
}
