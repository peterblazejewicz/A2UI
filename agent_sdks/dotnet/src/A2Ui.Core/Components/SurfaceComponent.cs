using System.Text.Json.Serialization;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Components;

/// <summary>Root surface container (extension).</summary>
public sealed record SurfaceComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "Surface";
}
