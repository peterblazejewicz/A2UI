using System.Text.Json.Serialization;
using A2Ui.Core.Bindings;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Components.Extensions;

/// <summary>
/// Data table component — project-local extension, NOT part of the v0.9 basic catalog.
/// Included in <see cref="A2UiComponent"/>'s <see cref="JsonDerivedTypeAttribute"/> list so
/// agents emitting <c>"component":"Table"</c> deserialize cleanly, but this type has no spec
/// counterpart and may be removed or restructured during a spec upgrade.
/// </summary>
public sealed record TableComponent : A2UiComponent
{
    /// <inheritdoc />
    [JsonIgnore]
    public override string Component { get; init; } = "Table";

    /// <summary>Table column definitions.</summary>
    [JsonPropertyName("columns")]
    public TableColumn[]? Columns { get; init; }

    /// <summary>Table row data reference.</summary>
    [JsonPropertyName("rows")]
    public DynamicValue? Rows { get; init; }
}
