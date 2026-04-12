using System.Text.Json.Serialization;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Components;

/// <summary>Text input field component.</summary>
public sealed record TextFieldComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "TextField";

    /// <summary>Current input value (bound to data model).</summary>
    [JsonPropertyName("value")]
    public DynamicValue? Value { get; init; }

    /// <summary>Client-side validation regular expression.</summary>
    [JsonPropertyName("validationRegexp")]
    public string? ValidationRegexp { get; init; }

    /// <summary>Number of visible text rows (for longText variant).</summary>
    [JsonPropertyName("rows")]
    public DynamicValue? Rows { get; init; }

    /// <summary>TextField variant: longText, number, shortText, obscured.</summary>
    [JsonPropertyName("variant")]
    public string? Variant { get; init; }
}
