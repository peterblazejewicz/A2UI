using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>ACTIVITY_SNAPSHOT — full snapshot of activity state for a message.</summary>
public sealed record ActivitySnapshotEvent : BaseEvent
{
    /// <summary>Identifier of the message this activity is associated with.</summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }

    /// <summary>Type of activity (application-defined).</summary>
    [JsonPropertyName("activityType")]
    public string? ActivityType { get; init; }

    /// <summary>Complete activity state as a JSON element.</summary>
    [JsonPropertyName("activity")]
    public required JsonElement Activity { get; init; }

    /// <summary>When true, replaces existing activity state entirely.</summary>
    [JsonPropertyName("replace")]
    public bool? Replace { get; init; }
}
