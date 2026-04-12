using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Capabilities;

/// <summary>Version-specific client capabilities.</summary>
public sealed record ClientCapabilitiesV09
{
    /// <summary>Catalog identifiers that the client supports rendering.</summary>
    [JsonPropertyName("supportedCatalogIds")]
    public required string[] SupportedCatalogIds { get; init; }

    /// <summary>
    /// Inline catalog definitions. Only provided when the server declares
    /// <c>acceptsInlineCatalogs: true</c>. Represented as raw JSON to avoid
    /// modeling the full Catalog/$defs schema at this layer.
    /// </summary>
    [JsonPropertyName("inlineCatalogs")]
    public JsonElement[]? InlineCatalogs { get; init; }
}
