using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// A2UI v0.9 client-to-server message envelope.
/// Contains exactly one of: action or error.
/// </summary>
public sealed record ClientToServerMessage
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = "v0.9";

    [JsonPropertyName("action")]
    public ClientAction? Action { get; init; }

    [JsonPropertyName("error")]
    public ClientError? Error { get; init; }

    /// <summary>
    /// Validate that this message conforms to the A2UI v0.9 client envelope constraints:
    /// version must be "v0.9" and exactly one of action or error must be present.
    /// </summary>
    /// <exception cref="A2UiMessageValidationException">Thrown when constraints are violated.</exception>
    public void Validate()
    {
        var violations = new List<string>();

        if (string.IsNullOrEmpty(Version))
            violations.Add("Missing required 'version' property");
        else if (Version != "v0.9")
            violations.Add($"Unsupported version '{Version}'; expected 'v0.9'");

        bool hasAction = Action is not null;
        bool hasError = Error is not null;

        if (hasAction && hasError)
            violations.Add("Message contains both 'action' and 'error'; exactly one is allowed");
        else if (!hasAction && !hasError)
            violations.Add("Message must contain exactly one of 'action' or 'error'");

        if (violations.Count > 0)
            throw new A2UiMessageValidationException(violations);
    }
}

/// <summary>
/// Client-to-server action — triggered by user interaction with a component.
/// </summary>
public sealed record ClientAction
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("surfaceId")]
    public required string SurfaceId { get; init; }

    [JsonPropertyName("sourceComponentId")]
    public required string SourceComponentId { get; init; }

    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    [JsonPropertyName("context")]
    public required JsonElement Context { get; init; }
}

/// <summary>
/// Client-to-server error — validation failure or generic error.
/// </summary>
public sealed record ClientError
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("surfaceId")]
    public required string SurfaceId { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>JSON Pointer to the failed field. Only for VALIDATION_FAILED code.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; init; }
}
