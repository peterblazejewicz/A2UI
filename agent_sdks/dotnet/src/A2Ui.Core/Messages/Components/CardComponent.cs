using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Card container with rounded borders.</summary>
public sealed record CardComponent : A2UiComponent
{
    [JsonIgnore]
    public override string Component { get; init; } = "Card";
}
