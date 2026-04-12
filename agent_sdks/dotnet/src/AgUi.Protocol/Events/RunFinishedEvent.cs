using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>RUN_FINISHED — mandatory terminal event.</summary>
public sealed record RunFinishedEvent : BaseEvent
{
    /// <summary>Conversation thread identifier.</summary>
    [JsonPropertyName("threadId")]
    public required string ThreadId { get; init; }

    /// <summary>Unique identifier for this run.</summary>
    [JsonPropertyName("runId")]
    public required string RunId { get; init; }

    /// <summary>Optional result payload from the completed run.</summary>
    [JsonPropertyName("result")]
    public JsonElement? Result { get; init; }
}
