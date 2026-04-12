using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Choice picker (dropdown/radio/checkbox list).</summary>
public sealed record ChoicePickerComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "ChoicePicker";

    /// <summary>Current selected value(s) (bound to data model).</summary>
    [JsonPropertyName("value")]
    public DynamicValue? Value { get; init; }

    /// <summary>Available choice options.</summary>
    [JsonPropertyName("options")]
    public ChoiceOption[]? Options { get; init; }

    /// <summary>Display style for multiple selection: default or chips.</summary>
    [JsonPropertyName("displayStyle")]
    public string? DisplayStyle { get; init; }

    /// <summary>Whether the picker supports text filtering.</summary>
    [JsonPropertyName("filterable")]
    public bool? Filterable { get; init; }

    /// <summary>Picker variant: multipleSelection or mutuallyExclusive.</summary>
    [JsonPropertyName("variant")]
    public string? Variant { get; init; }
}
