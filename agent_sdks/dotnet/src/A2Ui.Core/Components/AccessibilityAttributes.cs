using System.Text.Json.Serialization;
using A2Ui.Core.Bindings;

namespace A2Ui.Core.Components;

/// <summary>Accessibility attributes for screen readers.</summary>
public sealed record AccessibilityAttributes
{
    /// <summary>Accessible label text for screen readers.</summary>
    [JsonPropertyName("label")]
    public DynamicValue? Label { get; init; }

    /// <summary>Accessible description text for screen readers.</summary>
    [JsonPropertyName("description")]
    public DynamicValue? Description { get; init; }
}
