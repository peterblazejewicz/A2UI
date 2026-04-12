using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>Root surface container (extension).</summary>
public sealed record SurfaceComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "Surface";
}
