using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>REASONING_START — opens a reasoning block.</summary>
public sealed record ReasoningStartEvent : BaseEvent
{
    /// <summary>Optional message identifier for the reasoning block.</summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }
}

/// <summary>REASONING_END — closes a reasoning block.</summary>
public sealed record ReasoningEndEvent : BaseEvent
{
    /// <summary>Optional message identifier for the reasoning block.</summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }
}

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

/// <summary>REASONING_MESSAGE_END — closes a reasoning message stream.</summary>
public sealed record ReasoningMessageEndEvent : BaseEvent
{
    /// <summary>Identifier of the reasoning message being closed.</summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; init; }
}

/// <summary>REASONING_MESSAGE_CHUNK — convenience event that auto-expands to Start, Content, End.</summary>
public sealed record ReasoningMessageChunkEvent : BaseEvent
{
    /// <summary>Message identifier; required on the first chunk.</summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }

    /// <summary>Reasoning text chunk to append.</summary>
    [JsonPropertyName("delta")]
    public string? Delta { get; init; }
}

/// <summary>
/// Carries encrypted reasoning value across turns.
/// Inspired by OpenAI encrypted reasoning items.
/// </summary>
public sealed record ReasoningEncryptedValueEvent : BaseEvent
{
    /// <summary>Whether this encrypted value is for a tool call or a message.</summary>
    [JsonPropertyName("subtype")]
    [JsonConverter(typeof(JsonStringEnumConverter<ReasoningSubtype>))]
    public required ReasoningSubtype Subtype { get; init; }

    /// <summary>Identifier of the entity (tool call or message) this value belongs to.</summary>
    [JsonPropertyName("entityId")]
    public required string EntityId { get; init; }

    /// <summary>Opaque encrypted reasoning value for cross-turn persistence.</summary>
    [JsonPropertyName("encryptedValue")]
    public required string EncryptedValue { get; init; }
}

/// <summary>Subtype discriminator for <see cref="ReasoningEncryptedValueEvent"/>.</summary>
public enum ReasoningSubtype
{
    /// <summary>Encrypted value associated with a tool call.</summary>
    [JsonStringEnumMemberName("tool-call")]
    ToolCall,

    /// <summary>Encrypted value associated with a message.</summary>
    [JsonStringEnumMemberName("message")]
    Message,
}
