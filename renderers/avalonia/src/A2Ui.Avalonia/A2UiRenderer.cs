using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using A2Ui.Avalonia.Catalog;
using A2Ui.Avalonia.Functions;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Threading;

namespace A2Ui.Avalonia;

/// <summary>
/// Renders an A2UI surface to Avalonia controls.
/// Must be called on the UI thread (Avalonia Dispatcher).
/// </summary>
public sealed class A2UiRenderer
{
    private readonly CatalogRegistry _catalog;
    private readonly IFunctionRegistry? _functionRegistry;
    private readonly Dictionary<string, Dictionary<string, Control>> _surfaceCaches = new();

    public A2UiRenderer(CatalogRegistry catalog, IFunctionRegistry? functionRegistry = null)
    {
        _catalog = catalog;
        _functionRegistry = functionRegistry;
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

        var context = new RenderContext(surface, _catalog, cache, _functionRegistry,
            (surfaceId, eventName, payload, componentId) =>
                UserActionFired?.Invoke(this, new(surfaceId, eventName, payload, componentId)),
            (surfaceId) =>
                DataModelChanged?.Invoke(this, new(surfaceId)));

        var roots = surface.GetRootComponents().ToList();

        if (roots.Count == 1)
            return RenderComponent(roots[0], surface, context);

        var container = new StackPanel { Spacing = 8 };
        foreach (var root in roots)
            container.Children.Add(RenderComponent(root, surface, context));
        return container;
    }

    public event EventHandler<UserActionEventArgs>? UserActionFired;
    public event EventHandler<DataModelChangedEventArgs>? DataModelChanged;

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
        if (cache is not null)
            cache[component.Id] = control;
        return control;
    }

    /// <summary>Clear control cache for a specific surface.</summary>
    public void ClearSurface(string surfaceId) => _surfaceCaches.Remove(surfaceId);
}

internal sealed class RenderContext(
    Surface surface,
    CatalogRegistry catalog,
    Dictionary<string, Control> cache,
    IFunctionRegistry? functionRegistry,
    Action<string, string, object?, string?> fireAction,
    Action<string> onDataModelChanged,
    string? basePath = null)
    : IRenderContext
{
    private const int MaxResolveDepth = 32;
    private static readonly JsonSerializerOptions s_jsonOptions = new();

    public Control? RenderChild(string? childId)
    {
        if (childId is null || !surface.Components.TryGetValue(childId, out var c))
            return null;

        if (!catalog.TryGetEntry(c.Component, out var entry) || entry is null)
        {
            return new TextBlock
            {
                Text = $"[Unknown component: {c.Component}]",
                Classes = { "Caption" },
            };
        }

        if (cache.TryGetValue(c.Id, out var existing) &&
            entry.Update(existing, c, surface.DataModel, this))
        {
            DetachFromParent(existing);
            return existing;
        }

        var control = entry.Create(c, surface.DataModel, this);
        cache[c.Id] = control;
        return control;
    }

    /// <summary>
    /// Detach a control from its current visual parent so it can be
    /// safely added to a new container. Handles Panel, ContentControl,
    /// and Decorator (Border) parent types.
    /// </summary>
    internal static void DetachFromParent(Control control)
    {
        switch (control.Parent)
        {
            case null:
                break;
            case Panel panel:
                panel.Children.Remove(control);
                break;
            case ContentControl cc when ReferenceEquals(cc.Content, control):
                cc.Content = null;
                break;
            case Decorator decorator when ReferenceEquals(decorator.Child, control):
                decorator.Child = null;
                break;
            default:
                Trace.TraceWarning(
                    $"[A2UiRenderer] Cannot detach control from unknown parent type {control.Parent.GetType().Name}");
                break;
        }
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

            if (comp.Children.Template is { } tmpl)
                return ExpandTemplate(tmpl);
        }

        // Legacy fallback: parent-based lookup
        return surface.Components.Values
            .Where(c => c.Parent == componentId)
            .Select(c => RenderChild(c.Id))
            .OfType<Control>();
    }

    /// <summary>
    /// Expand a template child list by iterating over the data model array
    /// at the template path and rendering the template component tree once
    /// per array item, with relative path resolution scoped to each item.
    /// </summary>
    private IEnumerable<Control> ExpandTemplate(ChildTemplate tmpl)
    {
        string arrayPath = tmpl.Path.TrimStart('/');
        int count = surface.DataModel.GetArrayLength(tmpl.Path);
        if (count <= 0)
            return [];

        if (!surface.Components.TryGetValue(tmpl.ComponentId, out var templateComp))
            return [];

        if (!catalog.TryGetEntry(templateComp.Component, out var entry) || entry is null)
            return [];

        var controls = new List<Control>(count);
        for (int i = 0; i < count; i++)
        {
            string itemBasePath = $"/{arrayPath}/{i}";

            // Each template instance gets its own cache so children rendered
            // from the same component IDs (e.g. rc_title) don't collide.
            var instanceCache = new Dictionary<string, Control>();
            var scopedContext = new RenderContext(
                surface, catalog, instanceCache, functionRegistry,
                fireAction, onDataModelChanged, itemBasePath);

            var control = entry.Create(templateComp, surface.DataModel, scopedContext);
            controls.Add(control);
        }

        return controls;
    }

    public void FireUserAction(string eventName, object? payload = null, string? componentId = null) =>
        fireAction(surface.SurfaceId, eventName, payload, componentId);

    public string? Resolve(DynamicValue? value) => ResolveCore(value, depth: 0);

    private string? ResolveCore(DynamicValue? value, int depth)
    {
        if (value is null)
            return null;

        if (depth > MaxResolveDepth)
        {
            Trace.TraceWarning(
                $"[RenderContext] Resolve exceeded max depth ({MaxResolveDepth}), returning null");
            return null;
        }

        if (value.FunctionCall is { } fc)
            return ResolveFunction(fc, depth);

        // ArrayLiteral: resolve each element individually and return as JSON string array.
        // This handles cases like and/or where "values" is an array of DynamicValues
        // (paths, function calls, etc.) that each need recursive resolution.
        if (value.ArrayLiteral is { } arrayEl)
            return ResolveArrayLiteral(arrayEl, depth);

        // Scope relative paths when inside a template expansion
        if (basePath is not null && value.Path is { } path && !path.StartsWith('/'))
            return ResolveScopedPath(path);

        return surface.DataModel.Resolve(value);
    }

    /// <summary>
    /// Resolve a JSON array of DynamicValues by resolving each element individually.
    /// Returns a JSON string array, e.g. <c>["true","false","true"]</c>, suitable for
    /// functions like <c>and</c>/<c>or</c> that use <see cref="BuiltInFunctions.ParseJsonStringArray"/>.
    /// </summary>
    private string? ResolveArrayLiteral(JsonElement arrayEl, int depth)
    {
        if (arrayEl.ValueKind != JsonValueKind.Array)
            return null;

        var results = new List<string>();
        foreach (JsonElement item in arrayEl.EnumerateArray())
        {
            try
            {
                var itemDv = JsonSerializer.Deserialize<DynamicValue>(
                    item.GetRawText(), s_jsonOptions);
                string? resolved = ResolveCore(itemDv, depth + 1);
                results.Add(resolved ?? "");
            }
            catch (JsonException ex)
            {
                Trace.TraceWarning(
                    $"[RenderContext] Failed to deserialize array element: {ex.Message}");
                results.Add("");
            }
        }

        return JsonSerializer.Serialize(results);
    }

    private string? ResolveFunction(FunctionCallValue fc, int depth)
    {
        if (functionRegistry is null)
        {
            Trace.TraceWarning(
                $"[RenderContext] FunctionCall '{fc.Call}' encountered but no function registry configured");
            return null;
        }

        // Special case: formatString needs template parsing with resolver access
        if (fc.Call == "formatString")
            return ResolveFormatString(fc, depth);

        var resolvedArgs = new Dictionary<string, string?>();
        if (fc.Args is not null)
        {
            foreach (var (key, jsonEl) in fc.Args)
            {
                try
                {
                    var argValue = JsonSerializer.Deserialize<DynamicValue>(
                        jsonEl.GetRawText(), s_jsonOptions);
                    resolvedArgs[key] = ResolveCore(argValue, depth + 1);
                }
                catch (JsonException ex)
                {
                    Trace.TraceWarning(
                        $"[RenderContext] Failed to deserialize arg '{key}' for function '{fc.Call}': {ex.Message}");
                    resolvedArgs[key] = null;
                }
            }
        }
        return functionRegistry.Evaluate(fc.Call, resolvedArgs);
    }

    /// <summary>
    /// Handle <c>formatString</c> by parsing the template with <see cref="ExpressionParser"/>
    /// and resolving all embedded <c>${...}</c> tokens (paths, function calls, literals).
    /// </summary>
    private string? ResolveFormatString(FunctionCallValue fc, int depth)
    {
        // Get the raw "value" arg — it's a string literal containing ${} tokens
        string? template = null;
        if (fc.Args is not null && fc.Args.TryGetValue("value", out JsonElement valEl))
        {
            try
            {
                var valDv = JsonSerializer.Deserialize<DynamicValue>(valEl.GetRawText(), s_jsonOptions);
                // The template is always a string literal like "${formatDate(value: ${/start}, format: 'E, MMM d')}"
                template = valDv?.StringLiteral;
            }
            catch (JsonException ex)
            {
                Trace.TraceWarning(
                    $"[RenderContext] Failed to deserialize formatString 'value' arg: {ex.Message}");
            }
        }

        if (string.IsNullOrEmpty(template))
            return "";

        try
        {
            var parser = new ExpressionParser();
            IReadOnlyList<ExpressionToken> tokens = parser.Parse(template);

            var sb = new StringBuilder();
            foreach (ExpressionToken token in tokens)
            {
                string? resolved = ResolveExpressionToken(token, depth + 1);
                if (resolved is not null)
                    sb.Append(resolved);
            }
            return sb.ToString();
        }
        catch (A2UiExpressionException ex)
        {
            Trace.TraceWarning(
                $"[RenderContext] Failed to parse formatString template: {ex.Message}");
            return template;
        }
    }

    /// <summary>
    /// Resolve a single <see cref="ExpressionToken"/> to its string value.
    /// </summary>
    private string? ResolveExpressionToken(ExpressionToken token, int depth)
    {
        if (depth > MaxResolveDepth)
        {
            Trace.TraceWarning(
                $"[RenderContext] ResolveExpressionToken exceeded max depth ({MaxResolveDepth})");
            return null;
        }

        return token switch
        {
            LiteralToken lit => lit.Value,
            PathToken path => ResolveExpressionPath(path.Path, depth),
            BoolToken b => b.Value ? "true" : "false",
            NumberToken n => n.Value.ToString(CultureInfo.InvariantCulture),
            FunctionCallToken fc => ResolveExpressionFunctionCall(fc, depth),
            _ => null,
        };
    }

    /// <summary>
    /// Resolve a path token from an expression, respecting scoped base paths.
    /// </summary>
    private string? ResolveExpressionPath(string path, int depth)
    {
        // Scope relative paths when inside a template expansion
        if (basePath is not null && !path.StartsWith('/'))
            return ResolveScopedPath(path);

        return ResolveCore(DynamicValue.FromPath(path), depth);
    }

    /// <summary>
    /// Resolve a <see cref="FunctionCallToken"/> by resolving each arg token,
    /// then calling the function registry.
    /// </summary>
    private string? ResolveExpressionFunctionCall(FunctionCallToken fc, int depth)
    {
        if (functionRegistry is null)
            return null;

        var resolvedArgs = new Dictionary<string, string?>();
        foreach (var (key, argToken) in fc.Args)
        {
            resolvedArgs[key] = ResolveExpressionToken(argToken, depth + 1);
        }

        return functionRegistry.Evaluate(fc.Name, resolvedArgs);
    }

    private string? ResolveScopedPath(string path)
    {
        var scopedValue = DynamicValue.FromPath($"{basePath}/{path}");
        return surface.DataModel.Resolve(scopedValue);
    }

    public double? GetComponentWeight(string componentId) =>
        surface.Components.TryGetValue(componentId, out var comp) ? comp.Weight : null;

    public void UpdateDataModel(string path, string? value)
    {
        try
        {
            var update = new UpdateDataModel
            {
                SurfaceId = surface.SurfaceId,
                Path = path,
                Value = value is not null
                    ? JsonSerializer.SerializeToElement(value)
                    : null,
            };
            surface.DataModel.Apply(update);
            onDataModelChanged(surface.SurfaceId);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)
        {
            Trace.TraceWarning(
                $"[A2UiRenderer] Failed to update data model at path '{path}': {ex.Message}");
        }
    }
}

public sealed record UserActionEventArgs(
    string SurfaceId,
    string EventName,
    object? Payload,
    string? ComponentId = null);

public sealed record DataModelChangedEventArgs(string SurfaceId);
