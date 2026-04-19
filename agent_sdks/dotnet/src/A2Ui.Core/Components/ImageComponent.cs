using System.Text.Json.Serialization;
using A2Ui.Core.Bindings;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Components;

/// <summary>Image display component.</summary>
public sealed record ImageComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "Image";

    /// <summary>Image source URL.</summary>
    [JsonPropertyName("url")]
    public DynamicValue? Url { get; init; }

    /// <summary>Descriptive text (alt text) for the image.</summary>
    [JsonPropertyName("description")]
    public DynamicValue? Description { get; init; }

    /// <summary>Image fit mode: contain, cover, fill, none, scaleDown.</summary>
    [JsonPropertyName("fit")]
    public string? Fit { get; init; }

    /// <summary>Image variant: icon, avatar, smallFeature, etc.</summary>
    [JsonPropertyName("variant")]
    public string? Variant { get; init; }
}
