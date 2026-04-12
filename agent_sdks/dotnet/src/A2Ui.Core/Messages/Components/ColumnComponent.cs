using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Vertical layout container.</summary>
public sealed record ColumnComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "Column";

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
