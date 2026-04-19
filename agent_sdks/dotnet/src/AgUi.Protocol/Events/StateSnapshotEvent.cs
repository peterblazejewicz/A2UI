using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>
/// STATE_SNAPSHOT — frontend replaces its entire local state with this snapshot.
/// </summary>
public sealed record StateSnapshotEvent : BaseEvent
{
    /// <summary>
    /// Complete state snapshot as a JSON element.
    /// </summary>
    /// <remarks>
    /// Typed as <see cref="JsonElement"/> because agent state is application-defined
    /// (the AG-UI reference spec does not constrain the snapshot shape). Consumers
    /// deserialize into their own state DTO via <c>Snapshot.Deserialize&lt;TState&gt;()</c>
    /// or read individual properties structurally with
    /// <see cref="JsonElement.TryGetProperty(string, out JsonElement)"/>.
    /// </remarks>
    [JsonPropertyName("snapshot")]
    public required JsonElement Snapshot { get; init; }
}
