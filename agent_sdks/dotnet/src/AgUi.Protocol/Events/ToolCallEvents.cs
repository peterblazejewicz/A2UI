using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

public sealed record ToolCallStartEvent : BaseEvent
{
    [JsonPropertyName("toolCallId")]      public required string ToolCallId   { get; init; }
    [JsonPropertyName("toolCallName")]    public required string ToolCallName { get; init; }
    [JsonPropertyName("parentMessageId")] public string? ParentMessageId      { get; init; }
}

/// <summary>
/// Carries raw JSON string fragments. Concatenate all deltas for a toolCallId
/// to obtain the complete JSON arguments object.
/// </summary>
public sealed record ToolCallArgsEvent : BaseEvent
{
    [JsonPropertyName("toolCallId")] public required string ToolCallId { get; init; }
    [JsonPropertyName("delta")]      public required string Delta      { get; init; }
}

public sealed record ToolCallEndEvent : BaseEvent
{
    [JsonPropertyName("toolCallId")] public required string ToolCallId { get; init; }
}

public sealed record ToolCallResultEvent : BaseEvent
{
    [JsonPropertyName("messageId")]  public required string MessageId  { get; init; }
    [JsonPropertyName("toolCallId")] public required string ToolCallId { get; init; }
    [JsonPropertyName("content")]    public required JsonElement Content { get; init; }
    [JsonPropertyName("role")]       public string? Role               { get; init; }
}

/// <summary>Convenience: auto-expands to Start→Args→End.</summary>
public sealed record ToolCallChunkEvent : BaseEvent
{
    [JsonPropertyName("toolCallId")]      public string? ToolCallId      { get; init; }
    [JsonPropertyName("toolCallName")]    public string? ToolCallName    { get; init; }
    [JsonPropertyName("parentMessageId")] public string? ParentMessageId { get; init; }
    [JsonPropertyName("delta")]           public string? Delta           { get; init; }
}