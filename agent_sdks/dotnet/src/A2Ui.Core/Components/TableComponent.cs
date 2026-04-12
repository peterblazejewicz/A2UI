using System.Text.Json.Serialization;
using A2Ui.Core.Messages;

namespace A2Ui.Core.Components;

/// <summary>Data table component (extension).</summary>
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
