using System.Text.Json.Serialization;
using A2Ui.Core.Messages;
using A2Ui.Core.Tabs;

namespace A2Ui.Core.Components;

/// <summary>Tabbed container component.</summary>
public sealed record TabsComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "Tabs";

    /// <summary>Tab definitions with title and child reference.</summary>
    [JsonPropertyName("tabs")]
    public TabDefinition[]? Tabs { get; init; }
}
