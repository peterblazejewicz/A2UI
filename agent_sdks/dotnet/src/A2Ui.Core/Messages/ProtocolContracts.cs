using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// A2UI v0.9 server capabilities — advertised via transport metadata
/// (e.g., A2A AgentCard params, MCP initialization).
/// Maps to specification/v0_9/json/server_capabilities.json.
/// </summary>
public sealed record ServerCapabilities
{
    /// <summary>Version 0.9 capabilities.</summary>
    [JsonPropertyName("v0.9")]
    public required ServerCapabilitiesV09 V09 { get; init; }
}

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

/// <summary>
/// A2UI v0.9 client capabilities — sent from client to server as transport metadata
/// (e.g., A2A message metadata field "a2uiClientCapabilities").
/// Maps to specification/v0_9/json/client_capabilities.json.
/// </summary>
public sealed record ClientCapabilities
{
    /// <summary>Version 0.9 capabilities.</summary>
    [JsonPropertyName("v0.9")]
    public required ClientCapabilitiesV09 V09 { get; init; }
}

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

/// <summary>
/// A2UI v0.9 client data model — attached to client-to-server message metadata
/// when a surface has <c>sendDataModel: true</c>.
/// Maps to specification/v0_9/json/client_data_model.json.
/// </summary>
public sealed record ClientDataModel
{
    /// <summary>Protocol version string (always "v0.9").</summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = "v0.9";

    /// <summary>Map of surfaceId → current data model JSON for that surface.</summary>
    [JsonPropertyName("surfaces")]
    public required Dictionary<string, JsonElement> Surfaces { get; init; }
}
