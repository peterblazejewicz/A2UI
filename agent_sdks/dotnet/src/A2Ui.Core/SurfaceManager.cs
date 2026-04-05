using System.Text.Json;
using A2Ui.Core.Messages;

namespace A2Ui.Core;

/// <summary>
/// Manages the lifecycle of A2UI surfaces and their component trees.
/// Thread-safe via locking; call from dispatcher thread in UI layer.
/// </summary>
public sealed class SurfaceManager
{
    private readonly Dictionary<string, Surface> _surfaces = new();
    private readonly object _lock = new();

    public event EventHandler<SurfaceCreatedEventArgs>?    SurfaceCreated;
    public event EventHandler<SurfaceDeletedEventArgs>?    SurfaceDeleted;
    public event EventHandler<ComponentsUpdatedEventArgs>? ComponentsUpdated;
    public event EventHandler<DataModelUpdatedEventArgs>?  DataModelUpdated;

    public void Process(A2UiMessage message)
    {
        lock (_lock)
        {
            if (message.CreateSurface is { } cs)    HandleCreate(cs);
            if (message.DeleteSurface is { } ds)    HandleDelete(ds);
            if (message.UpdateComponents is { } uc) HandleUpdateComponents(uc);
            if (message.UpdateDataModel  is { } ud) HandleUpdateDataModel(ud);
        }
    }

    public Surface? GetSurface(string surfaceId)
    {
        lock (_lock) { return _surfaces.GetValueOrDefault(surfaceId); }
    }

    private void HandleCreate(CreateSurface cs)
    {
        if (_surfaces.ContainsKey(cs.SurfaceId)) return;
        var surface = new Surface(cs.SurfaceId, cs.CatalogId)
        {
            Theme = cs.Theme,
            SendDataModel = cs.SendDataModel ?? false,
        };
        _surfaces[cs.SurfaceId] = surface;
        SurfaceCreated?.Invoke(this, new(surface));
    }

    private void HandleDelete(DeleteSurface ds)
    {
        if (_surfaces.Remove(ds.SurfaceId, out var surface))
            SurfaceDeleted?.Invoke(this, new(surface));
    }

    private void HandleUpdateComponents(UpdateComponents uc)
    {
        if (!_surfaces.TryGetValue(uc.SurfaceId, out var surface)) return;
        surface.UpdateComponents(uc.Components);
        ComponentsUpdated?.Invoke(this, new(surface, uc.Components));
    }

    private void HandleUpdateDataModel(UpdateDataModel ud)
    {
        if (!_surfaces.TryGetValue(ud.SurfaceId, out var surface)) return;
        surface.DataModel.Apply(ud);
        DataModelUpdated?.Invoke(this, new(surface));
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

    public IEnumerable<A2UiComponent> GetRootComponents() =>
        _components.Values.Where(c => c.Parent is null);
}

public sealed record SurfaceCreatedEventArgs(Surface Surface);
public sealed record SurfaceDeletedEventArgs(Surface Surface);
public sealed record ComponentsUpdatedEventArgs(Surface Surface, A2UiComponent[] Updated);
public sealed record DataModelUpdatedEventArgs(Surface Surface);
