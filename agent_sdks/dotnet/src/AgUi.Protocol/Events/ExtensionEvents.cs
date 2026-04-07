using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>RAW — passthrough from external systems.</summary>
public sealed record RawEvent : BaseEvent
{
    [JsonPropertyName("event")]
    public required JsonElement Event { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }
}

/// <summary>CUSTOM — application-defined extension event.</summary>
public sealed record CustomEvent : BaseEvent
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("value")]
    public required JsonElement Value { get; init; }
}
