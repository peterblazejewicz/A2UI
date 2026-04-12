using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>RUN_STARTED — mandatory first event. Establishes execution context.</summary>
public sealed record RunStartedEvent : BaseEvent
{
    /// <summary>Conversation thread identifier.</summary>
    [JsonPropertyName("threadId")]
    public required string ThreadId { get; init; }

    /// <summary>Unique identifier for this run.</summary>
    [JsonPropertyName("runId")]
    public required string RunId { get; init; }

    /// <summary>Parent run identifier for nested/sub-agent runs.</summary>
    [JsonPropertyName("parentRunId")]
    public string? ParentRunId { get; init; }

    /// <summary>Optional input payload for the run.</summary>
    [JsonPropertyName("input")]
    public JsonElement? Input { get; init; }
}
