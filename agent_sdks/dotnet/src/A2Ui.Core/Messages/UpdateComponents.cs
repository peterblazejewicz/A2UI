using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Operation payload for updating components on a surface.</summary>
public sealed record UpdateComponents
{
    /// <summary>Identifier of the target surface.</summary>
    [JsonPropertyName("surfaceId")]
    public required string SurfaceId { get; init; }

    /// <summary>Components to add or update in the surface's component tree.</summary>
    [JsonPropertyName("components")]
    public required A2UiComponent[] Components { get; init; }
}
