using System.Text.Json.Serialization;

namespace A2Ui.Core.Components;

/// <summary>Action: server event or client function call.</summary>
public sealed record ComponentAction
{
    /// <summary>Server-side event to fire when the action is triggered.</summary>
    [JsonPropertyName("event")]
    public ActionEvent? Event { get; init; }

    /// <summary>Client-side function call to execute when the action is triggered.</summary>
    [JsonPropertyName("functionCall")]
    public FunctionCallValue? FunctionCall { get; init; }
}
