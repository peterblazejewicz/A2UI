using System.Text.Json.Serialization;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Components;

/// <summary>Icon display component.</summary>
public sealed record IconComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "Icon";

    /// <summary>Icon name (mapped to a Unicode symbol).</summary>
    [JsonPropertyName("name")]
    public DynamicValue? Name { get; init; }
}
