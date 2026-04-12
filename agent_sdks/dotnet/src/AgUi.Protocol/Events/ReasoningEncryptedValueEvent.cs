using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

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
