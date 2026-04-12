using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Tabbed container component.</summary>
public sealed record TabsComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Tabs";

    /// <summary>Tab definitions with title and child reference.</summary>
    [JsonPropertyName("tabs")]
    public TabDefinition[]? Tabs { get; init; }
}
