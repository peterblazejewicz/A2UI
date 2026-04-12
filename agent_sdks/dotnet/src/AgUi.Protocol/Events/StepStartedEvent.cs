using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>STEP_STARTED — optional sub-run progress.</summary>
public sealed record StepStartedEvent : BaseEvent
{
    /// <summary>Name of the step being started.</summary>
    [JsonPropertyName("stepName")]
    public required string StepName { get; init; }
}
