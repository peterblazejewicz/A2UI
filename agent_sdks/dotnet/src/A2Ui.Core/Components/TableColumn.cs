using System.Text.Json.Serialization;

namespace A2Ui.Core.Components;

/// <summary>Table column definition (extension, not in v0.9 spec).</summary>
public sealed record TableColumn
{
    /// <summary>Column header display text.</summary>
    [JsonPropertyName("header")]
    public required string Header { get; init; }

    /// <summary>Data field name used to extract cell values from row data.</summary>
    [JsonPropertyName("field")]
    public required string Field { get; init; }
}
