using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>MESSAGES_SNAPSHOT — full snapshot of the conversation message history.</summary>
public sealed record MessagesSnapshotEvent : BaseEvent
{
    /// <summary>Complete array of conversation messages.</summary>
    [JsonPropertyName("messages")]
    public required JsonElement[] Messages { get; init; }
}
