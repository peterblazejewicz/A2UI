using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// A2UI v0.9 server-to-client message — one JSONL line on the wire.
/// Each message contains exactly one top-level operation key.
/// </summary>
public sealed record A2UiMessage
{
    /// <summary>Protocol version string (must be "v0.9").</summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    /// <summary>Create a new surface with the given catalog and theme.</summary>
    [JsonPropertyName("createSurface")]
    public CreateSurface? CreateSurface { get; init; }

    /// <summary>Delete an existing surface by its identifier.</summary>
    [JsonPropertyName("deleteSurface")]
    public DeleteSurface? DeleteSurface { get; init; }

    /// <summary>Update the component tree on an existing surface.</summary>
    [JsonPropertyName("updateComponents")]
    public UpdateComponents? UpdateComponents { get; init; }

    /// <summary>Update the data model on an existing surface.</summary>
    [JsonPropertyName("updateDataModel")]
    public UpdateDataModel? UpdateDataModel { get; init; }

    /// <summary>
    /// Validate that this message conforms to the A2UI v0.9 envelope constraints:
    /// version must be "v0.9" and exactly one operation must be present.
    /// </summary>
    /// <exception cref="A2UiMessageValidationException">Thrown when constraints are violated.</exception>
    public void Validate()
    {
        var violations = new List<string>();

        if (Version is null)
            violations.Add("Missing required 'version' property");
        else if (Version != "v0.9")
            violations.Add($"Unsupported version '{Version}'; expected 'v0.9'");

        int opCount =
            (CreateSurface is not null ? 1 : 0)
            + (DeleteSurface is not null ? 1 : 0)
            + (UpdateComponents is not null ? 1 : 0)
            + (UpdateDataModel is not null ? 1 : 0);

        if (opCount == 0)
            violations.Add(
                "Message must contain exactly one operation (createSurface, deleteSurface, updateComponents, or updateDataModel)"
            );
        else if (opCount > 1)
            violations.Add($"Message contains {opCount} operations; exactly one is allowed");

        if (violations.Count > 0)
            throw new A2UiMessageValidationException(violations);
    }

    /// <summary>Returns which operation is set, or null if none/multiple.</summary>
    [JsonIgnore]
    public A2UiOperationType? Operation =>
        this switch
        {
            { CreateSurface: not null } => A2UiOperationType.CreateSurface,
            { DeleteSurface: not null } => A2UiOperationType.DeleteSurface,
            { UpdateComponents: not null } => A2UiOperationType.UpdateComponents,
            { UpdateDataModel: not null } => A2UiOperationType.UpdateDataModel,
            _ => null,
        };
}

/// <summary>Discriminator for the operation type in an A2UiMessage.</summary>
public enum A2UiOperationType
{
    /// <summary>The message creates a new surface.</summary>
    CreateSurface,

    /// <summary>The message deletes an existing surface.</summary>
    DeleteSurface,

    /// <summary>The message updates the component tree on a surface.</summary>
    UpdateComponents,

    /// <summary>The message updates the data model on a surface.</summary>
    UpdateDataModel,
}

/// <summary>Operation payload for creating a new surface.</summary>
public sealed record CreateSurface
{
    /// <summary>Unique identifier for the surface being created.</summary>
    [JsonPropertyName("surfaceId")]
    public required string SurfaceId { get; init; }

    /// <summary>Catalog identifier defining the allowed component set.</summary>
    [JsonPropertyName("catalogId")]
    public required string CatalogId { get; init; }

    /// <summary>Optional theme configuration (primaryColor, iconUrl, agentDisplayName).</summary>
    [JsonPropertyName("theme")]
    public JsonElement? Theme { get; init; }

    /// <summary>When true, the client sends data model state back to the server.</summary>
    [JsonPropertyName("sendDataModel")]
    public bool? SendDataModel { get; init; }
}

/// <summary>Operation payload for deleting an existing surface.</summary>
public sealed record DeleteSurface
{
    /// <summary>Identifier of the surface to delete.</summary>
    [JsonPropertyName("surfaceId")]
    public required string SurfaceId { get; init; }
}

/// <summary>Operation payload for updating components on a surface.</summary>
public sealed record UpdateComponents
{
    /// <summary>Identifier of the target surface.</summary>
    [JsonPropertyName("surfaceId")]
    public required string SurfaceId { get; init; }

    /// <summary>Components to add or update in the surface's component tree.</summary>
    [JsonPropertyName("components")]
    public required A2UiComponent[] Components { get; init; }
}

/// <summary>
/// Updates the data model at a JSON Pointer path.
/// If path is null or "/", replaces the entire model.
/// If value is null, deletes the key at path.
/// </summary>
public sealed record UpdateDataModel
{
    /// <summary>Identifier of the target surface.</summary>
    [JsonPropertyName("surfaceId")]
    public required string SurfaceId { get; init; }

    /// <summary>JSON Pointer path; null or "/" replaces the entire model.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; init; }

    /// <summary>Value to set at the path; null deletes the key.</summary>
    [JsonPropertyName("value")]
    public JsonElement? Value { get; init; }
}
