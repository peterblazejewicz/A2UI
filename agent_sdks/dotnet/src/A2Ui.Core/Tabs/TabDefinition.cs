using System.Text.Json.Serialization;
using A2Ui.Core.Bindings;

namespace A2Ui.Core.Tabs;

/// <summary>Tab definition for Tabs component.</summary>
public sealed record TabDefinition
{
    /// <summary>
    /// Display title for the tab. The spec types this as <c>DynamicString</c>
    /// (see <c>specification/v0_9/json/basic_catalog.json</c>): values can be
    /// literal strings, path references, or function calls. Resolve through
    /// <c>IRenderContext.Resolve</c> before binding to a visual control.
    /// </summary>
    [JsonPropertyName("title")]
    public required DynamicValue Title { get; init; }

    /// <summary>Component ID of the tab's content.</summary>
    [JsonPropertyName("child")]
    public required string Child { get; init; }
}
