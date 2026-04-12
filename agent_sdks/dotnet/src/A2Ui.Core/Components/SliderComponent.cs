using System.Text.Json.Serialization;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Components;

/// <summary>Slider input component.</summary>
public sealed record SliderComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "Slider";

    /// <summary>Current slider value (bound to data model).</summary>
    [JsonPropertyName("value")]
    public DynamicValue? Value { get; init; }

    /// <summary>Minimum slider value.</summary>
    [JsonPropertyName("min")]
    public DynamicValue? Min { get; init; }

    /// <summary>Maximum slider value.</summary>
    [JsonPropertyName("max")]
    public DynamicValue? Max { get; init; }
}
