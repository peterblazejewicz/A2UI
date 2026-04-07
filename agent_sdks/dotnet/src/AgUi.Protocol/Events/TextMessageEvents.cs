using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

public sealed record TextMessageStartEvent : BaseEvent
{
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    [JsonPropertyName("role")]
    public string Role { get; init; } = "assistant";
}

public sealed record TextMessageContentEvent : BaseEvent
{
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }

    /// <summary>Non-empty text chunk. Concatenate all chunks for a messageId.</summary>
    [JsonPropertyName("delta")]
    public required string Delta { get; init; }
}

public sealed record TextMessageEndEvent : BaseEvent
{
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }
}

/// <summary>
/// Convenience event: auto-expands to Start→Content→End.
/// messageId required on first chunk; role defaults to "assistant".
/// </summary>
public sealed record TextMessageChunkEvent : BaseEvent
{
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }

    [JsonPropertyName("role")]
    public string? Role { get; init; }

    [JsonPropertyName("delta")]
    public string? Delta { get; init; }
}
