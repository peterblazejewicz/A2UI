using System.Text.Json.Serialization;

namespace A2Ui.Core.Components;

/// <summary>
/// A2UI v0.9 ChildList — either a static array of component IDs
/// or a template for dynamic list generation from the data model.
///
/// Wire formats:
///   ["id1", "id2", "id3"]                    → static Ids
///   {"componentId": "tmpl", "path": "/items"} → template
/// </summary>
[JsonConverter(typeof(ChildListConverter))]
public sealed record ChildList
{
    /// <summary>Static list of child component IDs.</summary>
    public string[]? Ids { get; init; }

    /// <summary>Template for dynamic children from a data model array.</summary>
    public ChildTemplate? Template { get; init; }

    /// <summary>Returns <see langword="true"/> when this instance uses a template for dynamic children.</summary>
    public bool IsTemplate => Template is not null;

    /// <summary>Creates a <see cref="ChildList"/> from a static array of component IDs.</summary>
    /// <param name="ids">Component identifiers of the children.</param>
    /// <returns>A new <see cref="ChildList"/> with static IDs.</returns>
    public static ChildList FromIds(params string[] ids) => new() { Ids = ids };

    /// <summary>Creates a <see cref="ChildList"/> from a template for data-driven children.</summary>
    /// <param name="componentId">Component ID of the template to repeat.</param>
    /// <param name="path">JSON Pointer path to the data model array.</param>
    /// <returns>A new <see cref="ChildList"/> with a template definition.</returns>
    public static ChildList FromTemplate(string componentId, string path) =>
        new()
        {
            Template = new ChildTemplate { ComponentId = componentId, Path = path },
        };
}
