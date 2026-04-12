using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// Template for generating dynamic children by iterating over a data model array.
/// </summary>
public sealed record ChildTemplate
{
    /// <summary>Component ID of the template to instantiate for each array element.</summary>
    [JsonPropertyName("componentId")]
    public required string ComponentId { get; init; }

    /// <summary>JSON Pointer path to the data model array driving repetition.</summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }
}
