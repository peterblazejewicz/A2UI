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
        _logger = (loggerFactory ?? NullLoggerFactory.Instance)
                      .CreateLogger<SurfaceManager>();
    }

    public event EventHandler<SurfaceCreatedEventArgs>?    SurfaceCreated;
    public event EventHandler<SurfaceDeletedEventArgs>?    SurfaceDeleted;
    public event EventHandler<ComponentsUpdatedEventArgs>? ComponentsUpdated;
    public event EventHandler<DataModelUpdatedEventArgs>?  DataModelUpdated;

    public void Process(A2UiMessage message)
    {
        // Collect event args inside the lock, fire outside
        SurfaceCreatedEventArgs?    createdArgs    = null;
        SurfaceDeletedEventArgs?    deletedArgs    = null;
        ComponentsUpdatedEventArgs? componentsArgs = null;
        DataModelUpdatedEventArgs?  dataModelArgs  = null;

        lock (_lock)
        {
            if (message.CreateSurface is { } cs)
                createdArgs = HandleCreate(cs);
            if (message.DeleteSurface is { } ds)
                deletedArgs = HandleDelete(ds);
            if (message.UpdateComponents is { } uc)
                componentsArgs = HandleUpdateComponents(uc);
            if (message.UpdateDataModel is { } ud)
                dataModelArgs = HandleUpdateDataModel(ud);
        }

        // Fire events outside the lock — safe for subscribers to call GetSurface
        if (createdArgs is not null)
        {
            SurfaceManagerLog.SurfaceCreated(_logger, createdArgs.Surface.SurfaceId);
            SurfaceCreated?.Invoke(this, createdArgs);
        }
        if (deletedArgs is not null)
        {
            SurfaceManagerLog.SurfaceDeleted(_logger, deletedArgs.Surface.SurfaceId);
            SurfaceDeleted?.Invoke(this, deletedArgs);
        }
        if (componentsArgs is not null)
        {
            SurfaceManagerLog.ComponentsUpdated(_logger, componentsArgs.Surface.SurfaceId, componentsArgs.Updated.Length);
            ComponentsUpdated?.Invoke(this, componentsArgs);
        }
        if (dataModelArgs is not null)
        {
            SurfaceManagerLog.DataModelUpdated(_logger, dataModelArgs.Surface.SurfaceId);
            DataModelUpdated?.Invoke(this, dataModelArgs);
        }
    }

    public Surface? GetSurface(string surfaceId)
    {
        lock (_lock) { return _surfaces.GetValueOrDefault(surfaceId); }
    }

    private SurfaceCreatedEventArgs? HandleCreate(CreateSurface cs)
    {
        if (_surfaces.ContainsKey(cs.SurfaceId)) return null;
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
        return null;
    }

    private ComponentsUpdatedEventArgs? HandleUpdateComponents(UpdateComponents uc)
    {
        if (!_surfaces.TryGetValue(uc.SurfaceId, out var surface)) return null;
        surface.UpdateComponents(uc.Components);
        return new(surface, uc.Components);
    }

    private DataModelUpdatedEventArgs? HandleUpdateDataModel(UpdateDataModel ud)
    {
        if (!_surfaces.TryGetValue(ud.SurfaceId, out var surface)) return null;
        surface.DataModel.Apply(ud);
        return new(surface);
    }
}

public sealed class Surface(string surfaceId, string catalogId)
{
    public string       SurfaceId     { get; } = surfaceId;
    public string       CatalogId     { get; } = catalogId;
    public DataModel    DataModel     { get; } = new();
    public JsonElement? Theme         { get; internal set; }
    public bool         SendDataModel { get; internal set; }

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
            if (c.Child is not null) childIds.Add(c.Child);
            if (c.Children?.Ids is { } ids)
                foreach (var id in ids) childIds.Add(id);
            if (c.Children?.Template is { } tmpl)
                childIds.Add(tmpl.ComponentId);
            if (c.Trigger is not null) childIds.Add(c.Trigger);
            if (c.Content is not null) childIds.Add(c.Content);
            if (c.Tabs is { } tabs)
                foreach (var tab in tabs)
                    childIds.Add(tab.Child);
        }

        var roots = _components.Values.Where(c => !childIds.Contains(c.Id)).ToList();
        if (roots.Count > 0) return roots;

        return _components.Values.Where(c => c.Parent is null);
    }
}

public sealed record SurfaceCreatedEventArgs(Surface Surface);
public sealed record SurfaceDeletedEventArgs(Surface Surface);
public sealed record ComponentsUpdatedEventArgs(Surface Surface, A2UiComponent[] Updated);
public sealed record DataModelUpdatedEventArgs(Surface Surface);

internal static partial class SurfaceManagerLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Surface created: {SurfaceId}")]
    public static partial void SurfaceCreated(ILogger logger, string surfaceId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "Surface deleted: {SurfaceId}")]
    public static partial void SurfaceDeleted(ILogger logger, string surfaceId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug,
        Message = "Components updated on {SurfaceId}: {Count} component(s)")]
    public static partial void ComponentsUpdated(ILogger logger, string surfaceId, int count);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug,
        Message = "Data model updated on {SurfaceId}")]
    public static partial void DataModelUpdated(ILogger logger, string surfaceId);
}
