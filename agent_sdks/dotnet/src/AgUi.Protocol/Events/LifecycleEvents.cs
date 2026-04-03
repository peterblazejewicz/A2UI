using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>RUN_STARTED — mandatory first event. Establishes execution context.</summary>
public sealed record RunStartedEvent : BaseEvent
{
    [JsonPropertyName("threadId")]   public required string ThreadId  { get; init; }
    [JsonPropertyName("runId")]      public required string RunId     { get; init; }
    [JsonPropertyName("parentRunId")] public string? ParentRunId      { get; init; }
    [JsonPropertyName("input")]      public JsonElement? Input        { get; init; }
}

/// <summary>RUN_FINISHED — mandatory terminal event.</summary>
public sealed record RunFinishedEvent : BaseEvent
{
    [JsonPropertyName("threadId")]  public required string ThreadId { get; init; }
    [JsonPropertyName("runId")]     public required string RunId    { get; init; }
    [JsonPropertyName("result")]    public JsonElement? Result      { get; init; }
}

/// <summary>RUN_ERROR — unrecoverable error, terminates run.</summary>
public sealed record RunErrorEvent : BaseEvent
{
    [JsonPropertyName("message")]  public required string Message { get; init; }
    [JsonPropertyName("code")]     public string? Code             { get; init; }
}

/// <summary>STEP_STARTED — optional sub-run progress.</summary>
public sealed record StepStartedEvent : BaseEvent
{
    [JsonPropertyName("stepName")] public required string StepName { get; init; }
}

/// <summary>STEP_FINISHED — must match corresponding STEP_STARTED.</summary>
public sealed record StepFinishedEvent : BaseEvent
{
    [JsonPropertyName("stepName")] public required string StepName { get; init; }
}