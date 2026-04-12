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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2Ui.Avalonia;

/// <summary>
/// Renders an A2UI surface to Avalonia controls.
/// Must be called on the UI thread (Avalonia Dispatcher).
/// </summary>
public sealed class A2UiRenderer
{
    private readonly CatalogRegistry _catalog;
    private readonly IFunctionRegistry? _functionRegistry;
    private readonly ILogger<A2UiRenderer> _logger;
    private readonly Dictionary<string, Dictionary<string, Control>> _surfaceCaches = new();
    private readonly Dictionary<string, CancellationTokenSource> _surfaceCts = new();

    public A2UiRenderer(
        CatalogRegistry catalog,
        IFunctionRegistry? functionRegistry = null,
        ILoggerFactory? loggerFactory = null
    )
    {
        this._catalog = catalog;
        this._functionRegistry = functionRegistry;
        this._logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<A2UiRenderer>();
    }

    /// <summary>
    /// Render or update the complete surface.
    /// Returns the root control for insertion into the visual tree.
    /// </summary>
    public Control Render(Surface surface)
    {
        Dispatcher.UIThread.VerifyAccess();

        using Activity? activity = Diagnostics.RendererSource.StartActivity("Renderer.Render", ActivityKind.Internal);
        activity?.SetTag("a2ui.surface_id", surface.SurfaceId);
        activity?.SetTag("a2ui.catalog_id", surface.CatalogId);

        if (!this._surfaceCaches.TryGetValue(surface.SurfaceId, out var cache))
        {
            cache = new Dictionary<string, Control>();
            this._surfaceCaches[surface.SurfaceId] = cache;
        }

        if (!this._surfaceCts.TryGetValue(surface.SurfaceId, out var cts))
        {
            cts = new CancellationTokenSource();
            this._surfaceCts[surface.SurfaceId] = cts;
        }

        var context = new RenderContext(
            surface,
            this._catalog,
            cache,
            this._functionRegistry,
            (surfaceId, eventName, payload, componentId) =>
                UserActionFired?.Invoke(this, new(surfaceId, eventName, payload, componentId)),
            (surfaceId) => DataModelChanged?.Invoke(this, new(surfaceId)),
            logger: this._logger,
            surfaceCancellation: cts.Token
        );

        var roots = surface.GetRootComponents().ToList();

        if (roots.Count == 1)
        {
            return this.RenderComponent(roots[0], surface, context);
        }

        var container = new StackPanel { Spacing = 8 };
        foreach (var root in roots)
        {
            container.Children.Add(this.RenderComponent(root, surface, context));
        }

        return container;
    }

    public event EventHandler<UserActionEventArgs>? UserActionFired;
    public event EventHandler<DataModelChangedEventArgs>? DataModelChanged;

    private Control RenderComponent(A2UiComponent component, Surface surface, RenderContext context)
    {
        RendererLog.RenderComponent(this._logger, component.Id, component.Component, component.Parent ?? "(none)");

        if (!this._catalog.TryGetEntry(component.Component, out var entry) || entry is null)
        {
            RendererLog.UnknownComponentTypeRendered(this._logger, component.Id, component.Component);
            return new TextBlock { Text = $"[Unknown component: {component.Component}]", Classes = { "Caption" } };
        }

        var cache = this._surfaceCaches.GetValueOrDefault(surface.SurfaceId);
        if (
            cache is not null
            && cache.TryGetValue(component.Id, out var existing)
            && entry.Update(existing, component, surface.DataModel, context)
        )
        {
            return existing;
        }

        var control = entry.Create(component, surface.DataModel, context);
        if (cache is not null)
        {
            cache[component.Id] = control;
        }

        return control;
    }

    /// <summary>Clear control cache for a specific surface and cancel in-flight async operations.</summary>
    public void ClearSurface(string surfaceId)
    {
        this._surfaceCaches.Remove(surfaceId);

        if (this._surfaceCts.Remove(surfaceId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}

internal sealed class RenderContext(
    Surface surface,
    CatalogRegistry catalog,
    Dictionary<string, Control> cache,
    IFunctionRegistry? functionRegistry,
    Action<string, string, object?, string?> fireAction,
    Action<string> onDataModelChanged,
    ILogger? logger = null,
    CancellationToken surfaceCancellation = default,
    string? basePath = null
) : IRenderContext
{
    public ILogger? Logger => logger;
    public CancellationToken SurfaceCancellation => surfaceCancellation;
    private const int MaxResolveDepth = 32;
    private static readonly JsonSerializerOptions s_jsonOptions = new();
    private static readonly ExpressionParser s_expressionParser = new();

    public Control? RenderChild(string? childId)
    {
        if (childId is null || !surface.Components.TryGetValue(childId, out var c))
        {
            return null;
        }

        if (!catalog.TryGetEntry(c.Component, out var entry) || entry is null)
        {
            if (logger is not null)
            {
                RendererLog.UnknownComponentTypeRendered(logger, c.Id, c.Component);
            }

            return new TextBlock { Text = $"[Unknown component: {c.Component}]", Classes = { "Caption" } };
        }

        if (cache.TryGetValue(c.Id, out var existing) && entry.Update(existing, c, surface.DataModel, this))
        {
            DetachFromParent(existing, logger);
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
    internal static void DetachFromParent(Control control, ILogger? logger = null)
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
                if (logger is not null)
                {
                    RendererLog.CannotDetachFromUnknownParent(logger, control.Parent.GetType().Name);
                }

                break;
        }
    }

    /// <summary>
    /// Render children: prefers v0.9 forward-ref children property,
    /// falls back to legacy parent-based reverse lookup.
    /// </summary>
    public IEnumerable<Control> RenderChildren(string parentId)
    {
        if (surface.Components.TryGetValue(parentId, out var comp) && comp.Children is not null)
        {
            if (comp.Children.Ids is { } ids)
            {
                return ids.Select(id => this.RenderChild(id)).OfType<Control>();
            }

            if (comp.Children.Template is { } tmpl)
            {
                return this.ExpandTemplate(tmpl);
            }
        }

        // Legacy fallback: parent-based lookup
        return surface
            .Components.Values.Where(c => c.Parent == parentId)
            .Select(c => this.RenderChild(c.Id))
            .OfType<Control>();
    }

    /// <summary>
    /// Expand a template child list by iterating over the data model array
    /// at the template path and rendering the template component tree once
    /// per array item, with relative path resolution scoped to each item.
    /// </summary>
    private List<Control> ExpandTemplate(ChildTemplate tmpl)
    {
        string arrayPath = tmpl.Path.TrimStart('/');
        int count = surface.DataModel.GetArrayLength(tmpl.Path);
        if (count <= 0)
        {
            return [];
        }

        if (!surface.Components.TryGetValue(tmpl.ComponentId, out var templateComp))
        {
            return [];
        }

        if (!catalog.TryGetEntry(templateComp.Component, out var entry) || entry is null)
        {
            return [];
        }

        var controls = new List<Control>(count);
        for (int i = 0; i < count; i++)
        {
            string itemBasePath = $"/{arrayPath}/{i}";

            // Each template instance gets its own cache so children rendered
            // from the same component IDs (e.g. rc_title) don't collide.
            var instanceCache = new Dictionary<string, Control>();
            var scopedContext = new RenderContext(
                surface,
                catalog,
                instanceCache,
                functionRegistry,
                fireAction,
                onDataModelChanged,
                logger,
                surfaceCancellation,
                itemBasePath
            );

            var control = entry.Create(templateComp, surface.DataModel, scopedContext);
            controls.Add(control);
        }

        if (logger is not null)
        {
            RendererLog.TemplateInstantiated(logger, tmpl.ComponentId, templateComp.Component, controls.Count);
        }

        return controls;
    }

    public void FireUserAction(string eventName, object? payload = null, string? componentId = null) =>
        fireAction(surface.SurfaceId, eventName, payload, componentId);

    public string? Resolve(DynamicValue? value) => this.ResolveCore(value, depth: 0);

    private string? ResolveCore(DynamicValue? value, int depth)
    {
        if (value is null)
        {
            return null;
        }

        if (depth > MaxResolveDepth)
        {
            if (logger is not null)
            {
                RendererLog.ResolveExceededMaxDepth(logger, MaxResolveDepth);
            }

            return null;
        }

        if (value is DynamicValue.FunctionValue { Call: var fc })
        {
            return this.ResolveFunction(fc, depth);
        }

        // ArrayLiteral: resolve each element individually and return as JSON string array.
        // This handles cases like and/or where "values" is an array of DynamicValues
        // (paths, function calls, etc.) that each need recursive resolution.
        if (value is DynamicValue.ArrayValue { Value: var arrayEl })
        {
            return this.ResolveArrayLiteral(arrayEl, depth);
        }

        // Scope relative paths when inside a template expansion
        if (basePath is not null && value is DynamicValue.PathValue { DataPath: var path } && !path.StartsWith('/'))
        {
            return this.ResolveScopedPath(path);
        }

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
        {
            return null;
        }

        var results = new List<string>();
        foreach (JsonElement item in arrayEl.EnumerateArray())
        {
            try
            {
                var itemDv = JsonSerializer.Deserialize<DynamicValue>(item.GetRawText(), s_jsonOptions);
                string? resolved = this.ResolveCore(itemDv, depth + 1);
                results.Add(resolved ?? "");
            }
            catch (JsonException ex)
            {
                if (logger is not null)
                {
                    RendererLog.FailedToDeserializeArrayElement(logger, ex);
                }

                results.Add("");
            }
        }

        return JsonSerializer.Serialize(results);
    }

    private string? ResolveFunction(FunctionCallValue fc, int depth)
    {
        if (functionRegistry is null)
        {
            if (logger is not null)
            {
                RendererLog.NoFunctionRegistryForCall(logger, fc.Call);
            }

            return null;
        }

        // Special case: formatString needs template parsing with resolver access
        if (fc.Call == "formatString")
        {
            return this.ResolveFormatString(fc, depth);
        }

        var resolvedArgs = new Dictionary<string, string?>();
        if (fc.Args is not null)
        {
            foreach (var (key, jsonEl) in fc.Args)
            {
                try
                {
                    var argValue = JsonSerializer.Deserialize<DynamicValue>(jsonEl.GetRawText(), s_jsonOptions);
                    resolvedArgs[key] = this.ResolveCore(argValue, depth + 1);
                }
                catch (JsonException ex)
                {
                    if (logger is not null)
                    {
                        RendererLog.FailedToDeserializeFunctionArg(logger, key, fc.Call, ex);
                    }

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
                template = valDv is DynamicValue.StringValue sv ? sv.Value : null;
            }
            catch (JsonException ex)
            {
                if (logger is not null)
                {
                    RendererLog.FailedToDeserializeFormatStringArg(logger, ex);
                }
            }
        }

        if (string.IsNullOrEmpty(template))
        {
            return "";
        }

        try
        {
            IReadOnlyList<ExpressionToken> tokens = s_expressionParser.Parse(template);

            var sb = new StringBuilder();
            foreach (ExpressionToken token in tokens)
            {
                string? resolved = this.ResolveExpressionToken(token, depth + 1);
                if (resolved is not null)
                {
                    sb.Append(resolved);
                }
            }
            return sb.ToString();
        }
        catch (A2UiExpressionException ex)
        {
            if (logger is not null)
            {
                RendererLog.FailedToParseFormatStringTemplate(logger, ex);
            }

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
            if (logger is not null)
            {
                RendererLog.ExpressionTokenExceededMaxDepth(logger, MaxResolveDepth);
            }

            return null;
        }

        return token switch
        {
            LiteralToken lit => lit.Value,
            PathToken path => this.ResolveExpressionPath(path.Path, depth),
            BoolToken b => b.Value ? "true" : "false",
            NumberToken n => n.Value.ToString(CultureInfo.InvariantCulture),
            FunctionCallToken fc => this.ResolveExpressionFunctionCall(fc, depth),
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
        {
            return this.ResolveScopedPath(path);
        }

        return this.ResolveCore(DynamicValue.FromPath(path), depth);
    }

    /// <summary>
    /// Resolve a <see cref="FunctionCallToken"/> by resolving each arg token,
    /// then calling the function registry.
    /// </summary>
    private string? ResolveExpressionFunctionCall(FunctionCallToken fc, int depth)
    {
        if (functionRegistry is null)
        {
            if (logger is not null)
            {
                RendererLog.NoFunctionRegistryForExpression(logger, fc.Name);
            }

            return null;
        }

        var resolvedArgs = new Dictionary<string, string?>();
        foreach (var (key, argToken) in fc.Args)
        {
            resolvedArgs[key] = this.ResolveExpressionToken(argToken, depth + 1);
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
                Value = value is not null ? JsonSerializer.SerializeToElement(value) : null,
            };
            surface.DataModel.Apply(update);
            onDataModelChanged(surface.SurfaceId);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)
        {
            if (logger is not null)
            {
                RendererLog.FailedToUpdateDataModel(logger, path, ex);
            }
        }
    }
}

public sealed record UserActionEventArgs(
    string SurfaceId,
    string EventName,
    object? Payload,
    string? ComponentId = null
);

public sealed record DataModelChangedEventArgs(string SurfaceId);

internal static partial class RendererLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Cannot detach control from unknown parent type {ParentTypeName}"
    )]
    public static partial void CannotDetachFromUnknownParent(ILogger logger, string parentTypeName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Resolve exceeded max depth ({MaxDepth}), returning null"
    )]
    public static partial void ResolveExceededMaxDepth(ILogger logger, int maxDepth);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Failed to deserialize array element for function arg"
    )]
    public static partial void FailedToDeserializeArrayElement(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "FunctionCall '{FunctionName}' encountered but no function registry configured"
    )]
    public static partial void NoFunctionRegistryForCall(ILogger logger, string functionName);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Failed to deserialize arg '{ArgName}' for function '{FunctionName}'"
    )]
    public static partial void FailedToDeserializeFunctionArg(
        ILogger logger,
        string argName,
        string functionName,
        Exception exception
    );

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Failed to deserialize formatString 'value' arg")]
    public static partial void FailedToDeserializeFormatStringArg(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Failed to parse formatString template")]
    public static partial void FailedToParseFormatStringTemplate(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message = "ResolveExpressionToken exceeded max depth ({MaxDepth})"
    )]
    public static partial void ExpressionTokenExceededMaxDepth(ILogger logger, int maxDepth);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Warning,
        Message = "Expression function '{FunctionName}' encountered but no function registry configured"
    )]
    public static partial void NoFunctionRegistryForExpression(ILogger logger, string functionName);

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "Failed to update data model at path '{Path}'")]
    public static partial void FailedToUpdateDataModel(ILogger logger, string path, Exception exception);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Debug,
        Message = "Render component {ComponentId} (type={ComponentType}, parent={ParentComponentId})"
    )]
    public static partial void RenderComponent(
        ILogger logger,
        string componentId,
        string componentType,
        string parentComponentId
    );

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Warning,
        Message = "Unknown component type rendered as fallback: id={ComponentId}, type={ComponentType}"
    )]
    public static partial void UnknownComponentTypeRendered(ILogger logger, string componentId, string componentType);

    [LoggerMessage(
        EventId = 13,
        Level = LogLevel.Debug,
        Message = "Template instantiated: {ComponentId} (template={TemplateType}) × {InstanceCount}"
    )]
    public static partial void TemplateInstantiated(
        ILogger logger,
        string componentId,
        string templateType,
        int instanceCount
    );
}
