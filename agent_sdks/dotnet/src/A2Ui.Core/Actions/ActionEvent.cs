using System.Text.Json.Serialization;
using A2Ui.Core.Bindings;

namespace A2Ui.Core.Actions;

/// <summary>Server-side event triggered by user interaction.</summary>
public sealed record ActionEvent
{
    /// <summary>Event name that identifies the action on the server.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Optional key-value context data sent with the action event.</summary>
    [JsonPropertyName("context")]
    public Dictionary<string, DynamicValue>? Context { get; init; }
}
