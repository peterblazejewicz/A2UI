using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>REASONING_START — opens a reasoning block.</summary>
public sealed record ReasoningStartEvent : BaseEvent
{
    /// <summary>Optional message identifier for the reasoning block.</summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }
}
