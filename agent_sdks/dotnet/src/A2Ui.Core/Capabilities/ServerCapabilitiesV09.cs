using System.Text.Json.Serialization;

namespace A2Ui.Core.Capabilities;

/// <summary>Version-specific server capabilities.</summary>
public sealed record ServerCapabilitiesV09
{
    /// <summary>Catalog identifiers that the server supports.</summary>
    [JsonPropertyName("supportedCatalogIds")]
    public string[]? SupportedCatalogIds { get; init; }

    /// <summary>Whether the server accepts inline catalog definitions from the client.</summary>
    [JsonPropertyName("acceptsInlineCatalogs")]
    public bool AcceptsInlineCatalogs { get; init; }
}
