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

/// <summary>
/// STATE_DELTA — RFC 6902 JSON Patch operations array.
/// Apply to current state in order.
/// Operations: add, remove, replace, move, copy, test.
/// </summary>
public sealed record StateDeltaEvent : BaseEvent
{
    /// <summary>Array of RFC 6902 JSON Patch operations to apply in order.</summary>
    [JsonPropertyName("delta")]
    public required JsonElement[] Delta { get; init; }
}

/// <summary>MESSAGES_SNAPSHOT — full snapshot of the conversation message history.</summary>
public sealed record MessagesSnapshotEvent : BaseEvent
{
    /// <summary>Complete array of conversation messages.</summary>
    [JsonPropertyName("messages")]
    public required JsonElement[] Messages { get; init; }
}

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

/// <summary>ACTIVITY_DELTA — RFC 6902 JSON Patch operations applied to activity state.</summary>
public sealed record ActivityDeltaEvent : BaseEvent
{
    /// <summary>Identifier of the message this activity delta targets.</summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }

    /// <summary>Type of activity (application-defined).</summary>
    [JsonPropertyName("activityType")]
    public string? ActivityType { get; init; }

    /// <summary>Array of RFC 6902 JSON Patch operations to apply.</summary>
    [JsonPropertyName("patch")]
    public required JsonElement[] Patch { get; init; }
}
