using System.Diagnostics;
using System.Text.Json;
using A2Ui.Core;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2Ui.Core.Surfaces;

/// <summary>
/// Manages the lifecycle of A2UI surfaces and their component trees.
/// Thread-safe via locking. Events are fired outside the lock to prevent
/// deadlocks if subscribers call back into GetSurface or Process.
/// </summary>
public sealed class SurfaceManager
{
    private readonly Dictionary<string, Surface> _surfaces = [];

    // System.Threading.Lock (.NET 9+) is cheaper than `object` for the `lock` keyword
    // because the compiler emits Lock.EnterScope() / Dispose rather than Monitor.Enter/Exit,
    // avoiding the monitor-on-object allocation path.
    private readonly Lock _lock = new();
    private readonly ILogger<SurfaceManager> _logger;
    private readonly SurfaceManagerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SurfaceManager"/> class.
    /// </summary>
    /// <param name="loggerFactory">Optional logger factory for structured logging.</param>
    /// <param name="options">
    /// Optional policy controlling how spec-MUST violations are handled. Pass
    /// <c>new SurfaceManagerOptions { StrictMode = true }</c> to have the manager throw
    /// <see cref="A2UiMessageValidationException"/> at each leniency site instead of
    /// logging and continuing. Defaults to the lenient policy.
    /// </param>
    public SurfaceManager(ILoggerFactory? loggerFactory = null, SurfaceManagerOptions? options = null)
    {
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SurfaceManager>();
        _options = options ?? new SurfaceManagerOptions();
    }

    /// <summary>Raised when a new surface is created.</summary>
    public event EventHandler<SurfaceCreatedEventArgs>? SurfaceCreated;

    /// <summary>Raised when a surface is deleted.</summary>
    public event EventHandler<SurfaceDeletedEventArgs>? SurfaceDeleted;

    /// <summary>Raised when components are updated on a surface.</summary>
    public event EventHandler<ComponentsUpdatedEventArgs>? ComponentsUpdated;

    /// <summary>Raised when the data model is updated on a surface.</summary>
    public event EventHandler<DataModelUpdatedEventArgs>? DataModelUpdated;

    /// <summary>
    /// Processes a single A2UI message, updating internal state and raising events.
    /// </summary>
    /// <param name="message">The validated A2UI message to process.</param>
    /// <exception cref="A2UiMessageValidationException">Thrown when the message is invalid.</exception>
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

    /// <summary>Gets the surface with the specified identifier, or <see langword="null"/> if not found.</summary>
    /// <param name="surfaceId">The surface identifier to look up.</param>
    /// <returns>The surface, or <see langword="null"/>.</returns>
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
            if (_options.StrictMode)
            {
                throw new A2UiMessageValidationException(
                    $"Duplicate createSurface for surface '{cs.SurfaceId}' — spec requires each createSurface to target a new surface."
                );
            }
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
        if (_options.StrictMode)
        {
            throw new A2UiMessageValidationException($"deleteSurface references unknown surface '{ds.SurfaceId}'.");
        }
        return null;
    }

    private ComponentsUpdatedEventArgs? HandleUpdateComponents(UpdateComponents uc)
    {
        if (!_surfaces.TryGetValue(uc.SurfaceId, out var surface))
        {
            SurfaceManagerLog.UnknownSurfaceOp(_logger, "updateComponents", uc.SurfaceId);
            if (_options.StrictMode)
            {
                throw new A2UiMessageValidationException(
                    $"updateComponents references unknown surface '{uc.SurfaceId}'."
                );
            }
            return null;
        }
        surface.UpdateComponents(uc.Components);

        // Warn if the surface still has no "root" component after this update.
        // Spec requires exactly one component with id "root" (a2ui_protocol.md:320).
        if (!surface.Components.ContainsKey("root"))
        {
            SurfaceManagerLog.RootComponentMissing(_logger, uc.SurfaceId);
            if (_options.StrictMode)
            {
                throw new A2UiMessageValidationException(
                    $"Surface '{uc.SurfaceId}' has no 'root' component after updateComponents — spec requires exactly one."
                );
            }
        }

        return new(surface, uc.Components);
    }

    private DataModelUpdatedEventArgs? HandleUpdateDataModel(UpdateDataModel ud)
    {
        if (!_surfaces.TryGetValue(ud.SurfaceId, out var surface))
        {
            SurfaceManagerLog.UnknownSurfaceOp(_logger, "updateDataModel", ud.SurfaceId);
            if (_options.StrictMode)
            {
                throw new A2UiMessageValidationException(
                    $"updateDataModel references unknown surface '{ud.SurfaceId}'."
                );
            }
            return null;
        }

        try
        {
            surface.DataModel.Apply(ud);
        }
        catch (JsonException ex)
        {
            // DataModel.Apply rejects malformed paths (traverse-through-scalar,
            // non-object SetSnapshot, JsonArray+non-numeric leaf). Log at warning
            // and suppress the DataModelUpdated event so downstream observers
            // never see a "successful" notification for a failed update.
            SurfaceManagerLog.DataModelApplyFailed(_logger, ud.SurfaceId, ud.Path ?? "/", ex.Message);
            if (_options.StrictMode)
            {
                throw new A2UiMessageValidationException(
                    $"updateDataModel for surface '{ud.SurfaceId}' failed at path '{ud.Path ?? "/"}': {ex.Message}",
                    ex
                );
            }
            return null;
        }

        return new(surface);
    }
}
