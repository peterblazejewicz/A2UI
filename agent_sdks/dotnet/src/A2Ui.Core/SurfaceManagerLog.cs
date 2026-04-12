using Microsoft.Extensions.Logging;

namespace A2Ui.Core;

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
