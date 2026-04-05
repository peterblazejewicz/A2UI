using A2Ui.Rendering.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Threading;

namespace A2Ui.Rendering;

/// <summary>
/// Renders an A2UI surface to Avalonia controls.
/// Must be called on the UI thread (Avalonia Dispatcher).
/// </summary>
public sealed class A2UiRenderer
{
    private readonly CatalogRegistry _catalog;
    private readonly Dictionary<string, Control> _controlCache = new();

    public A2UiRenderer(CatalogRegistry catalog)
    {
        _catalog = catalog;
    }

    /// <summary>
    /// Render or update the complete surface.
    /// Returns the root control for insertion into the visual tree.
    /// </summary>
    public Control Render(Surface surface)
    {
        Dispatcher.UIThread.VerifyAccess();

        var context = new RenderContext(surface, _catalog, _controlCache,
            (surfaceId, eventName, payload) =>
                UserActionFired?.Invoke(this, new(surfaceId, eventName, payload)));

        // Render from root components
        var roots = surface.GetRootComponents().ToList();

        if (roots.Count == 1)
            return RenderComponent(roots[0], surface, context);

        var container = new StackPanel { Spacing = 8 };
        foreach (var root in roots)
            container.Children.Add(RenderComponent(root, surface, context));
        return container;
    }

    public event EventHandler<UserActionEventArgs>? UserActionFired;

    private Control RenderComponent(A2UiComponent component, Surface surface,
                                    RenderContext context)
    {
        if (!_catalog.TryGetEntry(component.Component, out var entry) || entry is null)
        {
            // Unknown type — render a placeholder
            return new TextBlock
            {
                Text    = $"[Unknown component: {component.Component}]",
                Classes = { "Caption" },
            };
        }

        // Try update in-place first (perf optimization)
        if (_controlCache.TryGetValue(component.Id, out var existing))
        {
            if (entry.Update(existing, component, surface.DataModel, context))
                return existing;
            // Update declined — fall through to recreate
        }

        var control = entry.Create(component, surface.DataModel, context);
        _controlCache[component.Id] = control;
        return control;
    }

    /// <summary>Clear control cache when surface is deleted.</summary>
    public void ClearSurface(string surfaceId) => _controlCache.Clear();
}

internal sealed class RenderContext(
    Surface surface,
    CatalogRegistry catalog,
    Dictionary<string, Control> cache,
    Action<string, string, object?> fireAction)
    : IRenderContext
{
    public Control? RenderChild(string? childId)
    {
        if (childId is null || !surface.Components.TryGetValue(childId, out var c))
            return null;

        if (!catalog.TryGetEntry(c.Component, out var entry) || entry is null)
            return null;

        if (cache.TryGetValue(c.Id, out var existing) &&
            entry.Update(existing, c, surface.DataModel, this))
            return existing;

        var control = entry.Create(c, surface.DataModel, this);
        cache[c.Id] = control;
        return control;
    }

    public IEnumerable<Control> RenderChildren(string parentId)
    {
        return surface.Components.Values
            .Where(c => c.Parent == parentId)
            .Select(c => RenderChild(c.Id))
            .OfType<Control>();
    }

    public void FireUserAction(string surfaceId, string eventName, object? payload = null) =>
        fireAction(surface.SurfaceId, eventName, payload);

    public string? Resolve(DynamicValue? value) =>
        surface.DataModel.Resolve(value);
}

public sealed record UserActionEventArgs(
    string SurfaceId,
    string EventName,
    object? Payload);