using Microsoft.Extensions.Logging;

namespace AgUi.Protocol;

internal static partial class ToolCallArgsAccumulatorLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Tool-call args accumulation started: toolCallId={ToolCallId}"
    )]
    public static partial void AccumulateStarted(ILogger logger, string toolCallId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Trace,
        Message = "Tool-call args delta appended: toolCallId={ToolCallId}, deltaLength={DeltaLength}, totalLength={TotalLength}"
    )]
    public static partial void AccumulateProgress(ILogger logger, string toolCallId, int deltaLength, int totalLength);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Tool-call args accumulation completed: toolCallId={ToolCallId}, totalLength={TotalLength}"
    )]
    public static partial void AccumulateCompleted(ILogger logger, string toolCallId, int totalLength);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Tool-call args orphaned (Complete never called): toolCallId={ToolCallId}, totalLength={TotalLength}"
    )]
    public static partial void AccumulateOrphaned(ILogger logger, string toolCallId, int totalLength);
}
