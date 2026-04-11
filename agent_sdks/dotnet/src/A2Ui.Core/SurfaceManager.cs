using System.Diagnostics;
using System.Text.Json;
using A2Ui.Core.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2Ui.Core;

/// <summary>
/// Manages the lifecycle of A2UI surfaces and their component trees.
/// Thread-safe via locking. Events are fired outside the lock to prevent
/// deadlocks if subscribers call back into GetSurface or Process.
/// </summary>
public sealed class SurfaceManager
{
    private readonly Dictionary<string, Surface> _surfaces = new();
    private readonly object _lock = new();
    private readonly ILogger<SurfaceManager> _logger;

    public SurfaceManager(ILoggerFactory? loggerFactory = null)
    {
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SurfaceManager>();
    }

    public event EventHandler<SurfaceCreatedEventArgs>? SurfaceCreated;
    public event EventHandler<SurfaceDeletedEventArgs>? SurfaceDeleted;
    public event EventHandler<ComponentsUpdatedEventArgs>? ComponentsUpdated;
    public event EventHandler<DataModelUpdatedEventArgs>? DataModelUpdated;

    public void Process(A2UiMessage message)
    {
        string messageType = message.Operation?.ToString() ?? "(empty)";
        string surfaceId =
            message.CreateSurface?.SurfaceId
            ?? message.DeleteSurface?.SurfaceId
            ?? message.UpdateComponents?.SurfaceId
            ?? message.UpdateDataModel?.SurfaceId
            ?? "(unknown)";
        SurfaceManagerLog.MessageDispatched(_logger, messageType, surfaceId);

        using Activity? activity = Diagnostics.Source.StartActivity($"Surface.{messageType}", ActivityKind.Internal);
        activity?.SetTag("a2ui.surface_id", surfaceId);
        activity?.SetTag("a2ui.message_type", messageType);

        using IDisposable? logScope = _logger.BeginScope(
            new Dictionary<string, object> { ["SurfaceId"] = surfaceId, ["MessageType"] = messageType }
        );

        try
        {
            message.Validate();
        }
        catch (A2UiMessageValidationException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            SurfaceManagerLog.ValidationFailed(_logger, messageType, ex.Message);
            throw;
        }

        // Collect event args AND telemetry snapshots inside the lock, fire outside.
        // The telemetry snapshots (root component, top-level data model keys, theme
        // extraction) are taken while holding `_lock` to avoid racing against concurrent
        // Process() / Clear() calls that mutate Surface.Components or DataModel._root.
        SurfaceCreatedEventArgs? createdArgs = null;
        SurfaceDeletedEventArgs? deletedArgs = null;
        ComponentsUpdatedEventArgs? componentsArgs = null;
        DataModelUpdatedEventArgs? dataModelArgs = null;
        string createdPrimaryColor = "(default)";
        string createdAgentDisplayName = "(default)";
        string componentsRootId = "(none)";
        string componentsRootType = "(none)";
        int dataModelPathCount = 0;
        string dataModelTopLevelKeys = string.Empty;

        lock (_lock)
        {
            if (message.CreateSurface is { } cs)
            {
                createdArgs = HandleCreate(cs);
                if (createdArgs is not null)
                {
                    (createdPrimaryColor, createdAgentDisplayName) = ExtractThemeFields(createdArgs.Surface.Theme);
                }
            }
            if (message.DeleteSurface is { } ds)
                deletedArgs = HandleDelete(ds);
            if (message.UpdateComponents is { } uc)
            {
                componentsArgs = HandleUpdateComponents(uc);
                if (componentsArgs is not null && componentsArgs.Surface.Components.TryGetValue("root", out var root))
                {
                    componentsRootId = root.Id;
                    componentsRootType = root.Component;
                }
            }
            if (message.UpdateDataModel is { } ud)
            {
                dataModelPathCount =
                    ud.Path?.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
                dataModelArgs = HandleUpdateDataModel(ud);
                if (dataModelArgs is not null)
                    dataModelTopLevelKeys = string.Join(",", dataModelArgs.Surface.DataModel.TopLevelKeys);
            }
        }

        // Fire events outside the lock — safe for subscribers to call GetSurface
        if (createdArgs is not null)
        {
            SurfaceManagerLog.SurfaceCreated(
                _logger,
                createdArgs.Surface.SurfaceId,
                createdArgs.Surface.CatalogId,
                createdPrimaryColor,
                createdAgentDisplayName
            );
            SurfaceCreated?.Invoke(this, createdArgs);
        }
        if (deletedArgs is not null)
        {
            SurfaceManagerLog.SurfaceDeleted(_logger, deletedArgs.Surface.SurfaceId);
            SurfaceDeleted?.Invoke(this, deletedArgs);
        }
        if (componentsArgs is not null)
        {
            SurfaceManagerLog.ComponentsUpdated(
                _logger,
                componentsArgs.Surface.SurfaceId,
                componentsArgs.Updated.Length,
                componentsRootId,
                componentsRootType
            );
            ComponentsUpdated?.Invoke(this, componentsArgs);
        }
        if (dataModelArgs is not null)
        {
            SurfaceManagerLog.DataModelUpdated(
                _logger,
                dataModelArgs.Surface.SurfaceId,
                dataModelPathCount,
                dataModelTopLevelKeys
            );
            DataModelUpdated?.Invoke(this, dataModelArgs);
        }
    }

    /// <summary>
    /// Extracts theme fields from the catalog theme object for telemetry.
    /// Spec schema (basic_catalog.json #/$defs/theme) defines primaryColor,
    /// iconUrl, and agentDisplayName. We surface primaryColor and
    /// agentDisplayName as they're the most human-meaningful for run comparison.
    /// </summary>
    private static (string PrimaryColor, string AgentDisplayName) ExtractThemeFields(JsonElement? theme)
    {
        if (theme is null || theme.Value.ValueKind != JsonValueKind.Object)
            return ("(default)", "(default)");

        string color =
            theme.Value.TryGetProperty("primaryColor", out var c) && c.ValueKind == JsonValueKind.String
                ? (c.GetString() ?? "(default)")
                : "(default)";
        string agent =
            theme.Value.TryGetProperty("agentDisplayName", out var a) && a.ValueKind == JsonValueKind.String
                ? (a.GetString() ?? "(default)")
                : "(default)";
        return (color, agent);
    }

    /// <summary>
    /// Removes all tracked surfaces and fires <see cref="SurfaceDeleted"/> for each.
    /// Used when a new prompt is sent and old surfaces should be discarded.
    /// </summary>
    public void Clear()
    {
        List<Surface> cleared;
        lock (_lock)
        {
            cleared = [.. _surfaces.Values];
            _surfaces.Clear();
        }

        foreach (var surface in cleared)
        {
            SurfaceManagerLog.SurfaceDeleted(_logger, surface.SurfaceId);
            SurfaceDeleted?.Invoke(this, new SurfaceDeletedEventArgs(surface));
        }

        if (cleared.Count > 0)
            SurfaceManagerLog.SurfacesCleared(_logger, cleared.Count);
    }

    public Surface? GetSurface(string surfaceId)
    {
        lock (_lock)
        {
            return _surfaces.GetValueOrDefault(surfaceId);
        }
    }

    private SurfaceCreatedEventArgs? HandleCreate(CreateSurface cs)
    {
        if (_surfaces.ContainsKey(cs.SurfaceId))
        {
            SurfaceManagerLog.DuplicateCreate(_logger, cs.SurfaceId);
            return null;
        }
        var surface = new Surface(cs.SurfaceId, cs.CatalogId)
        {
            Theme = cs.Theme,
            SendDataModel = cs.SendDataModel ?? false,
        };
        _surfaces[cs.SurfaceId] = surface;
        return new(surface);
    }

    private SurfaceDeletedEventArgs? HandleDelete(DeleteSurface ds)
    {
        if (_surfaces.Remove(ds.SurfaceId, out var surface))
            return new(surface);
        SurfaceManagerLog.UnknownSurfaceOp(_logger, "deleteSurface", ds.SurfaceId);
        return null;
    }

    private ComponentsUpdatedEventArgs? HandleUpdateComponents(UpdateComponents uc)
    {
        if (!_surfaces.TryGetValue(uc.SurfaceId, out var surface))
        {
            SurfaceManagerLog.UnknownSurfaceOp(_logger, "updateComponents", uc.SurfaceId);
            return null;
        }
        surface.UpdateComponents(uc.Components);

        // Warn if the surface still has no "root" component after this update.
        // Spec requires exactly one component with id "root" (a2ui_protocol.md:320).
        if (!surface.Components.ContainsKey("root"))
            SurfaceManagerLog.RootComponentMissing(_logger, uc.SurfaceId);

        return new(surface, uc.Components);
    }

    private DataModelUpdatedEventArgs? HandleUpdateDataModel(UpdateDataModel ud)
    {
        if (!_surfaces.TryGetValue(ud.SurfaceId, out var surface))
        {
            SurfaceManagerLog.UnknownSurfaceOp(_logger, "updateDataModel", ud.SurfaceId);
            return null;
        }
        surface.DataModel.Apply(ud);
        return new(surface);
    }
}

public sealed class Surface(string surfaceId, string catalogId)
{
    public string SurfaceId { get; } = surfaceId;
    public string CatalogId { get; } = catalogId;
    public DataModel DataModel { get; } = new();
    public JsonElement? Theme { get; internal set; }
    public bool SendDataModel { get; internal set; }

    private readonly Dictionary<string, A2UiComponent> _components = new();

    public IReadOnlyDictionary<string, A2UiComponent> Components => _components;

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
            if (c.Trigger is not null)
                childIds.Add(c.Trigger);
            if (c.Content is not null)
                childIds.Add(c.Content);
            if (c.Tabs is { } tabs)
                foreach (var tab in tabs)
                    childIds.Add(tab.Child);
        }

        var roots = _components.Values.Where(c => !childIds.Contains(c.Id)).ToList();
        if (roots.Count > 0)
            return roots;

        return _components.Values.Where(c => c.Parent is null);
    }
}

public sealed record SurfaceCreatedEventArgs(Surface Surface);

public sealed record SurfaceDeletedEventArgs(Surface Surface);

public sealed record ComponentsUpdatedEventArgs(Surface Surface, A2UiComponent[] Updated);

public sealed record DataModelUpdatedEventArgs(Surface Surface);

internal static partial class SurfaceManagerLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Surface created: {SurfaceId} (catalog={CatalogId}, primaryColor={PrimaryColor}, agentDisplayName={AgentDisplayName})"
    )]
    public static partial void SurfaceCreated(
        ILogger logger,
        string surfaceId,
        string catalogId,
        string primaryColor,
        string agentDisplayName
    );

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Surface deleted: {SurfaceId}")]
    public static partial void SurfaceDeleted(ILogger logger, string surfaceId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Components updated on {SurfaceId}: {Count} component(s), root={RootComponentId} ({RootComponentType})"
    )]
    public static partial void ComponentsUpdated(
        ILogger logger,
        string surfaceId,
        int count,
        string rootComponentId,
        string rootComponentType
    );

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Data model updated on {SurfaceId}: PathCount={PathCount}, TopLevelKeys=[{TopLevelKeys}]"
    )]
    public static partial void DataModelUpdated(ILogger logger, string surfaceId, int pathCount, string topLevelKeys);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Ignoring duplicate createSurface for '{SurfaceId}'"
    )]
    public static partial void DuplicateCreate(ILogger logger, string surfaceId);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Ignoring {Operation} for unknown surface '{SurfaceId}'"
    )]
    public static partial void UnknownSurfaceOp(ILogger logger, string operation, string surfaceId);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Warning,
        Message = "Surface '{SurfaceId}' has no component with id 'root'; renderer will use fallback heuristic"
    )]
    public static partial void RootComponentMissing(ILogger logger, string surfaceId);

    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Cleared {Count} surface(s)")]
    public static partial void SurfacesCleared(ILogger logger, int count);

    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "Dispatching {MessageType} for surface {SurfaceId}")]
    public static partial void MessageDispatched(ILogger logger, string messageType, string surfaceId);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Warning,
        Message = "A2UI message validation failed: {ValidationError} (type={MessageType})"
    )]
    public static partial void ValidationFailed(ILogger logger, string messageType, string validationError);
}
