using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Bindings;

/// <summary>
/// Invokes a named function on the client.
/// Maps to common_types.json#/$defs/FunctionCall.
/// </summary>
public sealed record FunctionCallValue
{
    /// <summary>Name of the client-side function to invoke.</summary>
    [JsonPropertyName("call")]
    public required string Call { get; init; }

    /// <summary>Optional named arguments passed to the function.</summary>
    [JsonPropertyName("args")]
    public Dictionary<string, JsonElement>? Args { get; init; }

    /// <summary>Optional expected return type hint (e.g., "string", "boolean").</summary>
    [JsonPropertyName("returnType")]
    public string? ReturnType { get; init; }
}
