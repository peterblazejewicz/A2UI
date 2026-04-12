using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Option for ChoicePicker component.</summary>
public sealed record ChoiceOption
{
    /// <summary>Display label for this choice option.</summary>
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    /// <summary>Data value submitted when this option is selected.</summary>
    [JsonPropertyName("value")]
    public required string Value { get; init; }
}
