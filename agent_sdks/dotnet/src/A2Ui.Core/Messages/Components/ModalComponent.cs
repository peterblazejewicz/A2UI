using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Modal dialog component.</summary>
public sealed record ModalComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Modal";

    /// <summary>ID of the trigger component that opens the modal.</summary>
    [JsonPropertyName("trigger")]
    public string? Trigger { get; init; }

    /// <summary>ID of the content component displayed inside the modal.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; init; }
}
