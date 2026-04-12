using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Operation payload for creating a new surface.</summary>
public sealed record CreateSurface
{
    /// <summary>Unique identifier for the surface being created.</summary>
    [JsonPropertyName("surfaceId")]
    public required string SurfaceId { get; init; }

    /// <summary>Catalog identifier defining the allowed component set.</summary>
    [JsonPropertyName("catalogId")]
    public required string CatalogId { get; init; }

    /// <summary>Optional theme configuration (primaryColor, iconUrl, agentDisplayName).</summary>
    [JsonPropertyName("theme")]
    public JsonElement? Theme { get; init; }

    /// <summary>When true, the client sends data model state back to the server.</summary>
    [JsonPropertyName("sendDataModel")]
    public bool? SendDataModel { get; init; }
}
