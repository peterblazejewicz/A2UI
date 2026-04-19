using System.Text.Json.Serialization;
using A2Ui.Core.Bindings;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Components;

/// <summary>Date/time input component.</summary>
public sealed record DateTimeInputComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "DateTimeInput";

    /// <summary>Current input value (bound to data model).</summary>
    [JsonPropertyName("value")]
    public DynamicValue? Value { get; init; }

    /// <summary>Whether the date picker is enabled (default true).</summary>
    [JsonPropertyName("enableDate")]
    public bool? EnableDate { get; init; }

    /// <summary>Whether the time picker is enabled (default false).</summary>
    [JsonPropertyName("enableTime")]
    public bool? EnableTime { get; init; }

    /// <summary>Minimum allowed date/time value.</summary>
    [JsonPropertyName("min")]
    public DynamicValue? Min { get; init; }

    /// <summary>Maximum allowed date/time value.</summary>
    [JsonPropertyName("max")]
    public DynamicValue? Max { get; init; }
}
