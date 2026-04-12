using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Icon display component.</summary>
public sealed record IconComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Icon";

    /// <summary>Icon name (mapped to a Unicode symbol).</summary>
    [JsonPropertyName("name")]
    public DynamicValue? Name { get; init; }
}
