using System.Text.Json;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;

namespace A2Ui.Core;

/// <summary>
/// Represents a live A2UI surface with its component tree and data model.
/// </summary>
/// <param name="surfaceId">Unique identifier for this surface.</param>
/// <param name="catalogId">Catalog identifier defining the allowed component set.</param>
public sealed class Surface(string surfaceId, string catalogId)
{
    /// <summary>Unique identifier for this surface.</summary>
    public string SurfaceId { get; } = surfaceId;

    /// <summary>Catalog identifier defining the allowed component set.</summary>
    public string CatalogId { get; } = catalogId;

    /// <summary>Per-surface data model store for two-way binding.</summary>
    public DataModel DataModel { get; } = new();

    /// <summary>Optional theme configuration from the createSurface message.</summary>
    public JsonElement? Theme { get; internal set; }

    /// <summary>Whether the client should send data model state back to the server.</summary>
    public bool SendDataModel { get; internal set; }

    private readonly Dictionary<string, A2UiComponent> _components = new();

    /// <summary>Read-only view of the component tree keyed by component ID.</summary>
    public IReadOnlyDictionary<string, A2UiComponent> Components => _components;

    /// <summary>Adds or replaces components in the surface's component tree.</summary>
    /// <param name="components">Components to upsert.</param>
    public void UpdateComponents(A2UiComponent[] components)
    {
        foreach (var c in components)
            _components[c.Id] = c;
    }

    /// <summary>
    /// Get root components. Prefers v0.9 convention (id == "root"), then falls
    /// back to components not referenced as children by any other component,
    /// then to legacy parent-is-null heuristic.
    /// </summary>
    public IEnumerable<A2UiComponent> GetRootComponents()
    {
        if (_components.TryGetValue("root", out var rootComp))
            return [rootComp];

        var childIds = new HashSet<string>();
        foreach (var c in _components.Values)
        {
            if (c.Child is not null)
                childIds.Add(c.Child);
            if (c.Children?.Ids is { } ids)
                foreach (var id in ids)
                    childIds.Add(id);
            if (c.Children?.Template is { } tmpl)
                childIds.Add(tmpl.ComponentId);
            if (c is ModalComponent modal)
            {
                if (modal.Trigger is not null)
                    childIds.Add(modal.Trigger);
                if (modal.Content is not null)
                    childIds.Add(modal.Content);
            }
            if (c is TabsComponent tabsComp && tabsComp.Tabs is { } tabs)
                foreach (var tab in tabs)
                    childIds.Add(tab.Child);
        }

        var roots = _components.Values.Where(c => !childIds.Contains(c.Id)).ToList();
        if (roots.Count > 0)
            return roots;

        return _components.Values.Where(c => c.Parent is null);
    }
}
