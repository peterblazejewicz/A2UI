using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>CUSTOM — application-defined extension event.</summary>
public sealed record CustomEvent : BaseEvent
{
    /// <summary>Application-defined event name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Application-defined event payload.</summary>
    [JsonPropertyName("value")]
    public required JsonElement Value { get; init; }
}
