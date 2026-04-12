using Microsoft.Extensions.Logging;

namespace AgUi.Protocol.Transport;

internal static partial class SseLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Skipping malformed SSE event: {Line}")]
    public static partial void MalformedEvent(ILogger logger, string line, Exception ex);
}
