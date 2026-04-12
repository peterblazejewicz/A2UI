using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>REASONING_END — closes a reasoning block.</summary>
public sealed record ReasoningEndEvent : BaseEvent
{
    /// <summary>Optional message identifier for the reasoning block.</summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }
}
