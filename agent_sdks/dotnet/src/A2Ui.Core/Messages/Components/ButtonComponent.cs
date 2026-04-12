using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Clickable button component.</summary>
public sealed record ButtonComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Button";

    /// <summary>Button variant: default, primary, borderless.</summary>
    [JsonPropertyName("variant")]
    public string? Variant { get; init; }

    /// <summary>Action triggered on click.</summary>
    [JsonPropertyName("action")]
    public ComponentAction? Action { get; init; }
}
