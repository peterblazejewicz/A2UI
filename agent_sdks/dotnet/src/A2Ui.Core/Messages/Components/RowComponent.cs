using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Horizontal layout container.</summary>
public sealed record RowComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Row";

    /// <summary>Main-axis justify mode: start, center, end, spaceBetween.</summary>
    [JsonPropertyName("justify")]
    public string? Justify { get; init; }

    /// <summary>Cross-axis alignment: start, center, end, stretch.</summary>
    [JsonPropertyName("align")]
    public string? Align { get; init; }

    /// <summary>Layout direction override.</summary>
    [JsonPropertyName("direction")]
    public string? Direction { get; init; }
}
