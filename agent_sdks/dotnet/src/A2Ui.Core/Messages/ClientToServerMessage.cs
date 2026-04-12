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
