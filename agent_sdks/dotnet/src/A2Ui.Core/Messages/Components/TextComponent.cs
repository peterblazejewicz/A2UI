using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Text display component.</summary>
public sealed record TextComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "Text";

    /// <summary>Text variant: h1, h2, h3, h4, h5, caption, body.</summary>
    [JsonPropertyName("variant")]
    public string? Variant { get; init; }

    /// <summary>Descriptive text for the component.</summary>
    [JsonPropertyName("description")]
    public DynamicValue? Description { get; init; }
}
