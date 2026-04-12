using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol.Events;

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
