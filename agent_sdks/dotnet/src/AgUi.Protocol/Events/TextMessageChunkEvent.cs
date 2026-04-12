using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>
/// Convenience event: auto-expands to Start→Content→End.
/// messageId required on first chunk; role defaults to "assistant".
/// </summary>
public sealed record TextMessageChunkEvent : BaseEvent
{
    /// <summary>Message identifier; required on the first chunk.</summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }

    /// <summary>Message role; defaults to "assistant" when omitted.</summary>
    [JsonPropertyName("role")]
    public string? Role { get; init; }

    /// <summary>Text chunk to append.</summary>
    [JsonPropertyName("delta")]
    public string? Delta { get; init; }
}
