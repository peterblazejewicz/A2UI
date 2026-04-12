using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>
/// STATE_SNAPSHOT — frontend replaces its entire local state with this snapshot.
/// </summary>
public sealed record StateSnapshotEvent : BaseEvent
{
    /// <summary>Complete state snapshot as a JSON element.</summary>
    [JsonPropertyName("snapshot")]
    public required JsonElement Snapshot { get; init; }
}
