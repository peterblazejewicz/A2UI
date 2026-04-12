using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// Client-to-server action — triggered by user interaction with a component.
/// </summary>
public sealed record ClientAction
{
    /// <summary>Action event name matching the component's action definition.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Identifier of the surface containing the source component.</summary>
    [JsonPropertyName("surfaceId")]
    public required string SurfaceId { get; init; }

    /// <summary>Component ID that triggered this action.</summary>
    [JsonPropertyName("sourceComponentId")]
    public required string SourceComponentId { get; init; }

    /// <summary>ISO 8601 timestamp of when the action was triggered.</summary>
    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    /// <summary>Action context data as a JSON element.</summary>
    [JsonPropertyName("context")]
    public required JsonElement Context { get; init; }
}
