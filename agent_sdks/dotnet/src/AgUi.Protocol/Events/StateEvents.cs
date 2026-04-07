using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>
/// STATE_SNAPSHOT — frontend replaces its entire local state with this snapshot.
/// </summary>
public sealed record StateSnapshotEvent : BaseEvent
{
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
    [JsonPropertyName("delta")]
    public required JsonElement[] Delta { get; init; }
}

public sealed record MessagesSnapshotEvent : BaseEvent
{
    [JsonPropertyName("messages")]
    public required JsonElement[] Messages { get; init; }
}

public sealed record ActivitySnapshotEvent : BaseEvent
{
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }

    [JsonPropertyName("activityType")]
    public string? ActivityType { get; init; }

    [JsonPropertyName("activity")]
    public required JsonElement Activity { get; init; }

    [JsonPropertyName("replace")]
    public bool? Replace { get; init; }
}

public sealed record ActivityDeltaEvent : BaseEvent
{
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }

    [JsonPropertyName("activityType")]
    public string? ActivityType { get; init; }

    [JsonPropertyName("patch")]
    public required JsonElement[] Patch { get; init; }
}
