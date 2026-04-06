using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;

namespace A2Ui.Avalonia.Catalog;

/// <summary>
/// Factory contract for a single A2UI component type.
/// Implement this for each type you add to the catalog.
/// </summary>
public interface ICatalogEntry
{
    /// <summary>
    /// A2UI component type string, e.g. "Text", "Button", "Column".
    /// Must exactly match the value in A2UiComponent.Component.
    /// </summary>
    string ComponentType { get; }

    /// <summary>
    /// Create an Avalonia control for this component.
    /// Called on the UI thread (Dispatcher).
    /// </summary>
    Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context);

    /// <summary>
    /// Update an existing control in-place (avoids full recreate).
    /// Return false to signal the renderer should recreate instead.
    /// </summary>
    bool Update(Control existing, A2UiComponent component, DataModel dataModel,
                IRenderContext context);
}

/// <summary>Context passed to factory methods for cross-cutting concerns.</summary>
public interface IRenderContext
{
    /// <summary>Render a child component by ID.</summary>
    Control? RenderChild(string? childId);

    /// <summary>Render all children of a component.</summary>
    IEnumerable<Control> RenderChildren(string parentId);

    /// <summary>Fire a user action event back to the agent. SurfaceId is resolved automatically.</summary>
    void FireUserAction(string eventName, object? payload = null, string? componentId = null);

    /// <summary>Resolve a DynamicValue from the data model.</summary>
    string? Resolve(DynamicValue? value);

    /// <summary>
    /// Write a value back to the data model at the given JSON Pointer path.
    /// Used for two-way binding (e.g. TextField with value: {"path": "/username"}).
    /// </summary>
    void UpdateDataModel(string path, string? value);

    /// <summary>
    /// Return the weight of a component by ID, or null if unknown / no weight set.
    /// Used by layout entries to build proportional star-sized grids.
    /// </summary>
    double? GetComponentWeight(string componentId);
}
