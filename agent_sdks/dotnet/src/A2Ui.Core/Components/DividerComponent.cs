using System.Text.Json.Serialization;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Components;

/// <summary>Visual divider (horizontal or vertical).</summary>
public sealed record DividerComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "Divider";

    /// <summary>Divider axis: horizontal (default) or vertical.</summary>
    [JsonPropertyName("axis")]
    public string? Axis { get; init; }
}
