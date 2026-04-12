using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

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
