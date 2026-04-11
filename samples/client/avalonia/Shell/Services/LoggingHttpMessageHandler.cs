using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace A2Ui.Avalonia.Shell.Services;

/// <summary>
/// <see cref="DelegatingHandler"/> that emits structured logs for every outbound
/// HTTP request sent through <see cref="HttpClient"/>. Complements
/// <see cref="A2AAgentClient"/>'s higher-level A2A-semantic logging: this
/// handler is the "raw network layer" view (URL, method, status, bytes,
/// duration) and fires regardless of which A2A method initiated the call.
/// </summary>
/// <remarks>
/// Registered via <c>AddHttpMessageHandler&lt;LoggingHttpMessageHandler&gt;</c>
/// on the typed <see cref="IA2AClient"/> in <c>Program.cs</c>. Uses
/// <see cref="LoggingHttpMessageHandlerLog"/> source-generated declarations
/// to avoid allocations at Debug level.
/// </remarks>
public sealed class LoggingHttpMessageHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHttpMessageHandler> _logger;

    public LoggingHttpMessageHandler(ILogger<LoggingHttpMessageHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        string method = request.Method.Method;
        string url = request.RequestUri?.ToString() ?? "(null)";

        long requestBytes = 0;
        if (request.Content is not null)
        {
            // ContentLength is set after the client code built the content
            // (e.g. ByteArrayContent in A2AAgentClient.SendAsync). If the
            // header isn't populated we fall back to -1 so operators can
            // distinguish "zero-length body" from "unknown length".
            requestBytes = request.Content.Headers.ContentLength ?? -1;
        }

        LoggingHttpMessageHandlerLog.RequestStarted(_logger, method, url, requestBytes);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            long responseBytes = response.Content.Headers.ContentLength ?? -1;
            LoggingHttpMessageHandlerLog.RequestCompleted(
                _logger,
                method,
                url,
                (int)response.StatusCode,
                responseBytes,
                stopwatch.ElapsedMilliseconds
            );

            return response;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            LoggingHttpMessageHandlerLog.RequestFailed(
                _logger,
                method,
                url,
                stopwatch.ElapsedMilliseconds,
                ex.GetType().Name,
                ex
            );
            throw;
        }
    }
}

internal static partial class LoggingHttpMessageHandlerLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "HTTP {Method} {Url} starting (requestBytes={RequestBytes})"
    )]
    public static partial void RequestStarted(ILogger logger, string method, string url, long requestBytes);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "HTTP {Method} {Url} completed: status={HttpStatus}, responseBytes={ResponseBytes}, durationMs={DurationMs}"
    )]
    public static partial void RequestCompleted(
        ILogger logger,
        string method,
        string url,
        int httpStatus,
        long responseBytes,
        long durationMs
    );

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "HTTP {Method} {Url} failed after {DurationMs}ms: {ExceptionType}"
    )]
    public static partial void RequestFailed(
        ILogger logger,
        string method,
        string url,
        long durationMs,
        string exceptionType,
        Exception exception
    );
}
