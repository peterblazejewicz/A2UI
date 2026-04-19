using System.Text.Json.Serialization;

namespace A2Ui.Core.Components.Extensions;

/// <summary>
/// Column definition for <see cref="TableComponent"/> — project-local extension companion,
/// NOT part of the v0.9 basic catalog. Lives alongside its owning component under
/// <see cref="Components.Extensions"/> because both share the same non-spec lifecycle and
/// may be removed or restructured during a spec upgrade.
/// </summary>
public sealed record TableColumn
{
    /// <summary>Column header display text.</summary>
    [JsonPropertyName("header")]
    public required string Header { get; init; }

    /// <summary>Data field name used to extract cell values from row data.</summary>
    [JsonPropertyName("field")]
    public required string Field { get; init; }
}
