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
    private readonly Dictionary<string, Dictionary<string, Control>> _surfaceCaches = new();

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

        if (!_surfaceCaches.TryGetValue(surface.SurfaceId, out var cache))
        {
            cache = new Dictionary<string, Control>();
            _surfaceCaches[surface.SurfaceId] = cache;
        }

        var context = new RenderContext(surface, _catalog, cache,
            (surfaceId, eventName, payload) =>
                UserActionFired?.Invoke(this, new(surfaceId, eventName, payload)));

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
            return new TextBlock
            {
                Text    = $"[Unknown component: {component.Component}]",
                Classes = { "Caption" },
            };
        }

        var cache = _surfaceCaches.GetValueOrDefault(surface.SurfaceId);
        if (cache is not null && cache.TryGetValue(component.Id, out var existing))
        {
            if (entry.Update(existing, component, surface.DataModel, context))
                return existing;
        }

        var control = entry.Create(component, surface.DataModel, context);
        cache?.TryAdd(component.Id, control);
        return control;
    }

    /// <summary>Clear control cache for a specific surface.</summary>
    public void ClearSurface(string surfaceId) => _surfaceCaches.Remove(surfaceId);
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

    /// <summary>
    /// Render children: prefers v0.9 forward-ref children property,
    /// falls back to legacy parent-based reverse lookup.
    /// </summary>
    public IEnumerable<Control> RenderChildren(string componentId)
    {
        if (surface.Components.TryGetValue(componentId, out var comp) &&
            comp.Children is not null)
        {
            if (comp.Children.Ids is { } ids)
                return ids.Select(id => RenderChild(id)).OfType<Control>();

            // Template children: stub — full expansion is future work
            if (comp.Children.Template is { } tmpl)
                return RenderChild(tmpl.ComponentId) is { } ctrl ? [ctrl] : [];
        }

        // Legacy fallback: parent-based lookup
        return surface.Components.Values
            .Where(c => c.Parent == componentId)
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
