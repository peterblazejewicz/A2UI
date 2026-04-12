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
