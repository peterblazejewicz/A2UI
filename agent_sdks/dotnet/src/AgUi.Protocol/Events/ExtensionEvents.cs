using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>RAW — passthrough from external systems.</summary>
public sealed record RawEvent : BaseEvent
{
    /// <summary>Raw event payload from the external system.</summary>
    [JsonPropertyName("event")]
    public required JsonElement Event { get; init; }

    /// <summary>Optional source identifier for the external system.</summary>
    [JsonPropertyName("source")]
    public string? Source { get; init; }
}

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
