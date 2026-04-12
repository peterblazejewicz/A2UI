using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Capabilities;

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
