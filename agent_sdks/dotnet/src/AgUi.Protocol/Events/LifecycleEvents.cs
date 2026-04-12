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

/// <summary>RUN_ERROR — unrecoverable error, terminates run.</summary>
public sealed record RunErrorEvent : BaseEvent
{
    /// <summary>Human-readable error message.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>Optional machine-readable error code.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }
}

/// <summary>STEP_STARTED — optional sub-run progress.</summary>
public sealed record StepStartedEvent : BaseEvent
{
    /// <summary>Name of the step being started.</summary>
    [JsonPropertyName("stepName")]
    public required string StepName { get; init; }
}

/// <summary>STEP_FINISHED — must match corresponding STEP_STARTED.</summary>
public sealed record StepFinishedEvent : BaseEvent
{
    /// <summary>Name of the step that finished.</summary>
    [JsonPropertyName("stepName")]
    public required string StepName { get; init; }
}
