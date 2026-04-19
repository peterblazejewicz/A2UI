using System.Text.Json.Serialization;
using A2Ui.Core.Bindings;
using A2Ui.Core.Messages;

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

    /// <summary>
    /// Assert the spec's one-of constraint: an action MUST have exactly one of
    /// <see cref="Event"/> or <see cref="FunctionCall"/> set
    /// (<c>specification/v0_9/json/common_types.json §Action</c>).
    /// </summary>
    /// <exception cref="A2UiMessageValidationException">
    /// Thrown when both are null, or when both are non-null.
    /// </exception>
    public void Validate()
    {
        bool hasEvent = Event is not null;
        bool hasCall = FunctionCall is not null;

        if (hasEvent == hasCall)
        {
            throw new A2UiMessageValidationException(
                hasEvent
                    ? "ComponentAction has both 'event' and 'functionCall' set — spec requires exactly one."
                    : "ComponentAction has neither 'event' nor 'functionCall' set — spec requires exactly one."
            );
        }
    }
}
