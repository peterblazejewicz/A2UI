using System.Text.Json.Serialization;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Components;

/// <summary>Scrollable list container.</summary>
public sealed record ListComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "List";

    /// <summary>List direction: vertical (default) or horizontal.</summary>
    [JsonPropertyName("direction")]
    public string? Direction { get; init; }

    /// <summary>Cross-axis alignment: start, center, end, stretch.</summary>
    [JsonPropertyName("align")]
    public string? Align { get; init; }
}
