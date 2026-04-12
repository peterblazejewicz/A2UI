using System.Text.Json.Serialization;

namespace A2Ui.Core.Capabilities;

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
