using Microsoft.Extensions.Logging;

namespace A2Ui.TestHelpers;

/// <summary>
/// A single captured log entry. <see cref="Properties"/> holds the
/// structured fields from a <c>[LoggerMessage]</c>-generated call site,
/// keyed by the template token name (e.g. <c>"SurfaceId"</c>).
/// </summary>
public sealed record TestLogEntry(
    string CategoryName,
    LogLevel Level,
    EventId EventId,
    string Message,
    IReadOnlyDictionary<string, object?> Properties,
    Exception? Exception
);
