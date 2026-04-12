using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Client-side validation rule.</summary>
public sealed record CheckRule
{
    /// <summary>Condition expression that must evaluate to true for the check to pass.</summary>
    [JsonPropertyName("condition")]
    public required DynamicValue Condition { get; init; }

    /// <summary>Error message displayed when the check fails.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
