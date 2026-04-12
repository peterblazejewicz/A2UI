using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Operation payload for deleting an existing surface.</summary>
public sealed record DeleteSurface
{
    /// <summary>Identifier of the surface to delete.</summary>
    [JsonPropertyName("surfaceId")]
    public required string SurfaceId { get; init; }
}
