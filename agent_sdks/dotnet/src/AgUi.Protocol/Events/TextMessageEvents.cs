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

/// <summary>TEXT_MESSAGE_END — closes a text message stream.</summary>
public sealed record TextMessageEndEvent : BaseEvent
{
    /// <summary>Identifier of the message being closed.</summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }
}

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
