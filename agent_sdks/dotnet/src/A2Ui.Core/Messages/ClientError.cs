using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

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
