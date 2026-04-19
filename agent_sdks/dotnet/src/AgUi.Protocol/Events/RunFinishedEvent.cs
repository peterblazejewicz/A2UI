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

    /// <summary>
    /// Optional result payload from the completed run.
    /// </summary>
    /// <remarks>
    /// Typed as <see cref="JsonElement"/>? to accept any shape an agent chooses to emit —
    /// strings, objects, arrays, or numbers are all valid per the AG-UI reference spec's
    /// unconstrained <c>result</c> field. Use <c>Result.Value.GetString()</c> / <c>.GetRawText()</c>
    /// depending on the agent's contract.
    /// </remarks>
    [JsonPropertyName("result")]
    public JsonElement? Result { get; init; }
}
