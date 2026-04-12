using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// A2UI v0.9 client-to-server message envelope.
/// Contains exactly one of: action or error.
/// </summary>
public sealed record ClientToServerMessage
{
    /// <summary>Protocol version string (always "v0.9").</summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = "v0.9";

    /// <summary>User action payload, mutually exclusive with <see cref="Error"/>.</summary>
    [JsonPropertyName("action")]
    public ClientAction? Action { get; init; }

    /// <summary>Client error payload, mutually exclusive with <see cref="Action"/>.</summary>
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
    /// <summary>Action event name matching the component's action definition.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Identifier of the surface containing the source component.</summary>
    [JsonPropertyName("surfaceId")]
    public required string SurfaceId { get; init; }

    /// <summary>Component ID that triggered this action.</summary>
    [JsonPropertyName("sourceComponentId")]
    public required string SourceComponentId { get; init; }

    /// <summary>ISO 8601 timestamp of when the action was triggered.</summary>
    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    /// <summary>Action context data as a JSON element.</summary>
    [JsonPropertyName("context")]
    public required JsonElement Context { get; init; }
}

/// <summary>
/// Client-to-server error — validation failure or generic error.
/// </summary>
public sealed record ClientError
{
    /// <summary>Machine-readable error code (e.g., "VALIDATION_FAILED").</summary>
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>Identifier of the surface where the error occurred.</summary>
    [JsonPropertyName("surfaceId")]
    public required string SurfaceId { get; init; }

    /// <summary>Human-readable error description.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>JSON Pointer to the failed field. Only for VALIDATION_FAILED code.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; init; }
}
