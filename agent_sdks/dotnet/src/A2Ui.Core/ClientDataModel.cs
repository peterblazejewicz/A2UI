using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core;

/// <summary>
/// A2UI v0.9 client data model — attached to client-to-server message metadata
/// when a surface has <c>sendDataModel: true</c>.
/// Maps to specification/v0_9/json/client_data_model.json.
/// </summary>
public sealed record ClientDataModel
{
    /// <summary>Protocol version string (always "v0.9").</summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = "v0.9";

    /// <summary>Map of surfaceId → current data model JSON for that surface.</summary>
    [JsonPropertyName("surfaces")]
    public required Dictionary<string, JsonElement> Surfaces { get; init; }
}
