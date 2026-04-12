using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

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
