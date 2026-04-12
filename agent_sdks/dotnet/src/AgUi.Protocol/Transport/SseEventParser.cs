using System.Text.Json;
using AgUi.Protocol.Events;
using Microsoft.Extensions.Logging;

namespace AgUi.Protocol.Transport;

/// <summary>
/// Parses AG-UI Server-Sent Events stream.
/// AG-UI uses ONLY the data: field; event: and id: fields are not used.
/// Each data: line contains one complete JSON event object.
/// </summary>
public static class SseEventParser
{
    private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Parses an SSE stream, yielding deserialized <see cref="BaseEvent"/> instances.
    /// Malformed events are logged and skipped (resilient parser).
    /// </summary>
    /// <param name="sseStream">The SSE byte stream to read from.</param>
    /// <param name="logger">Optional logger for malformed-event warnings.</param>
    /// <param name="cancellationToken">Cancellation token to stop parsing.</param>
    /// <returns>An async sequence of parsed AG-UI events.</returns>
    public static async IAsyncEnumerable<BaseEvent> ParseAsync(
        Stream sseStream,
        ILogger? logger = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        using var reader = new StreamReader(sseStream, leaveOpen: true);

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
                break;

            // SSE data line format: "data: {json}"
            if (!line.StartsWith("data:", StringComparison.Ordinal))
                continue;

            ReadOnlySpan<char> json = line.AsSpan(5).TrimStart();
            if (json.IsEmpty || json.SequenceEqual("[DONE]"))
                continue;

            BaseEvent? evt = null;
            try
            {
                evt = JsonSerializer.Deserialize<BaseEvent>(json, s_options);
            }
            catch (JsonException ex)
            {
                // Malformed event — skip, do not throw (resilient parser)
                if (logger is not null)
                {
                    SseLog.MalformedEvent(logger, line, ex);
                }
                continue;
            }

            if (evt is not null)
                yield return evt;
        }
    }
}

internal static partial class SseLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Skipping malformed SSE event: {Line}")]
    public static partial void MalformedEvent(ILogger logger, string line, Exception ex);
}
