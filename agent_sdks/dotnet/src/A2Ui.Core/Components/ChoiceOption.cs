using System.Text.Json.Serialization;

namespace A2Ui.Core.Components;

/// <summary>Option for ChoicePicker component.</summary>
public sealed record ChoiceOption
{
    /// <summary>
    /// Display label for this choice option. The spec types this as
    /// <c>DynamicString</c> (see <c>specification/v0_9/json/basic_catalog.json</c>):
    /// values can be literal strings, path references, or function calls.
    /// Resolve through <c>IRenderContext.Resolve</c> before binding.
    /// </summary>
    [JsonPropertyName("label")]
    public required DynamicValue Label { get; init; }

    /// <summary>Data value submitted when this option is selected.</summary>
    [JsonPropertyName("value")]
    public required string Value { get; init; }
}
