using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace A2Ui.Avalonia.Shell.Services;

/// <summary>
/// Listens for <c>A2A.SendMessage</c> <see cref="Activity"/> stop events
/// emitted by <see cref="A2AAgentClient"/> and projects each one into a
/// single Information-level <c>RequestSummary</c> log line with the key
/// fields operators need for scan-friendly log inspection.
/// </summary>
/// <remarks>
/// <para>
/// This complements <see cref="A2AAgentClient"/>'s per-step logs
/// (<c>SendMessageStarted</c>, <c>SendMessageCompleted</c>,
/// <c>SendMessageFailed</c>) — operators get both the narrative timeline
/// and the one-liner. The listener is registered as a DI singleton and
/// eager-constructed at startup so its <see cref="ActivityListener"/>
/// subscribes before the first request is sent.
/// </para>
/// <para>
/// The listener only samples <see cref="ActivitySource"/>s whose Name equals
/// <c>"A2Ui.Shell.A2AClient"</c>, and only emits a log when the Activity
/// operation name is <c>"A2A.SendMessage"</c>. This keeps the listener's
/// blast radius narrow — it does NOT interfere with any other
/// <c>ActivitySource</c> the host might have registered.
/// </para>
/// </remarks>
public sealed class RequestSummaryLogger : IDisposable
{
    private const string A2AClientSourceName = "A2Ui.Shell.A2AClient";
    private const string SendMessageActivityName = "A2A.SendMessage";

    private readonly ILogger<RequestSummaryLogger> _logger;
    private readonly ActivityListener _listener;

    public RequestSummaryLogger(ILogger<RequestSummaryLogger> logger)
    {
        this._logger = logger;
        this._listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == A2AClientSourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
            ActivityStopped = this.OnActivityStopped,
        };
        ActivitySource.AddActivityListener(this._listener);
    }

    private void OnActivityStopped(Activity activity)
    {
        if (activity.OperationName != SendMessageActivityName)
        {
            return;
        }

        string correlationId = GetTag(activity, "a2a.correlation_id") ?? "(none)";
        string httpUrl = GetTag(activity, "http.url") ?? "(none)";
        int httpStatus = TryParseInt(GetTag(activity, "http.status_code")) ?? 0;
        int messageCount = TryParseInt(GetTag(activity, "a2ui.message_count")) ?? 0;
        long durationMs = (long)activity.Duration.TotalMilliseconds;
        string status = activity.Status == ActivityStatusCode.Error ? "failed" : "ok";

        RequestSummaryLoggerLog.RequestSummary(
            this._logger,
            status,
            httpUrl,
            httpStatus,
            messageCount,
            durationMs,
            correlationId
        );
    }

    private static string? GetTag(Activity activity, string tagName)
    {
        foreach (var tag in activity.TagObjects)
        {
            if (tag.Key == tagName)
            {
                return tag.Value?.ToString();
            }
        }
        return null;
    }

    private static int? TryParseInt(string? s) => int.TryParse(s, CultureInfo.InvariantCulture, out int v) ? v : null;

    public void Dispose() => this._listener.Dispose();
}

internal static partial class RequestSummaryLoggerLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "RequestSummary: {Status} {HttpUrl} → httpStatus={HttpStatus}, messageCount={MessageCount}, durationMs={DurationMs}, correlationId={CorrelationId}"
    )]
    public static partial void RequestSummary(
        ILogger logger,
        string status,
        string httpUrl,
        int httpStatus,
        int messageCount,
        long durationMs,
        string correlationId
    );
}
