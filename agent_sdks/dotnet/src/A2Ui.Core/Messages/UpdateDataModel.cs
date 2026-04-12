using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// Updates the data model at a JSON Pointer path.
/// If path is null or "/", replaces the entire model.
/// If value is null, deletes the key at path.
/// </summary>
public sealed record UpdateDataModel
{
    /// <summary>Identifier of the target surface.</summary>
    [JsonPropertyName("surfaceId")]
    public required string SurfaceId { get; init; }

    /// <summary>JSON Pointer path; null or "/" replaces the entire model.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; init; }

    /// <summary>Value to set at the path; null deletes the key.</summary>
    [JsonPropertyName("value")]
    public JsonElement? Value { get; init; }
}
