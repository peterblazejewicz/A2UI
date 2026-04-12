using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>STEP_FINISHED — must match corresponding STEP_STARTED.</summary>
public sealed record StepFinishedEvent : BaseEvent
{
    /// <summary>Name of the step that finished.</summary>
    [JsonPropertyName("stepName")]
    public required string StepName { get; init; }
}
