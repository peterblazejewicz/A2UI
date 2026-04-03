using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

public sealed record ReasoningStartEvent       : BaseEvent { }
public sealed record ReasoningEndEvent         : BaseEvent { }

public sealed record ReasoningMessageStartEvent : BaseEvent
{
    [JsonPropertyName("messageId")] public required string MessageId { get; init; }
}

public sealed record ReasoningMessageContentEvent : BaseEvent
{
    [JsonPropertyName("messageId")] public required string MessageId { get; init; }
    [JsonPropertyName("delta")]     public required string Delta     { get; init; }
}

public sealed record ReasoningMessageEndEvent : BaseEvent
{
    [JsonPropertyName("messageId")] public required string MessageId { get; init; }
}

public sealed record ReasoningMessageChunkEvent : BaseEvent
{
    [JsonPropertyName("messageId")] public string? MessageId { get; init; }
    [JsonPropertyName("delta")]     public string? Delta     { get; init; }
}

/// <summary>
/// Carries encrypted reasoning value across turns.
/// Inspired by OpenAI encrypted reasoning items.
/// </summary>
public sealed record ReasoningEncryptedValueEvent : BaseEvent
{
    [JsonPropertyName("subtype")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ReasoningSubtype Subtype { get; init; }

    [JsonPropertyName("entityId")]       public required string EntityId       { get; init; }
    [JsonPropertyName("encryptedValue")] public required string EncryptedValue { get; init; }
}

public enum ReasoningSubtype { ToolCall, Message }