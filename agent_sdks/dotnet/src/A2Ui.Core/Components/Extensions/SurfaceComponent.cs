using System.Text.Json.Serialization;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Components.Extensions;

/// <summary>
/// Root surface container — project-local extension, NOT part of the v0.9 basic catalog.
/// Included in <see cref="A2UiComponent"/>'s <see cref="JsonDerivedTypeAttribute"/> list so
/// agents emitting <c>"component":"Surface"</c> deserialize cleanly, but this type has no
/// spec counterpart and may be removed or restructured during a spec upgrade.
/// </summary>
public sealed record SurfaceComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "Surface";
}
