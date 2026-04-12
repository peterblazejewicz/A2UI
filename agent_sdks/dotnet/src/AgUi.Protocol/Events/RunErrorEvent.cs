using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

/// <summary>RUN_ERROR — unrecoverable error, terminates run.</summary>
public sealed record RunErrorEvent : BaseEvent
{
    /// <summary>Human-readable error message.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>Optional machine-readable error code.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }
}
