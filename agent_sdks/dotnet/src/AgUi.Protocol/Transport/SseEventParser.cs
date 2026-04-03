using System.Text.Json;
using AgUi.Protocol.Events;

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

    public static async IAsyncEnumerable<BaseEvent> ParseAsync(
        Stream sseStream,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(sseStream, leaveOpen: true);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;

            // SSE data line format: "data: {json}"
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            ReadOnlySpan<char> json = line.AsSpan(5).TrimStart();
            if (json.IsEmpty || json.SequenceEqual("[DONE]")) continue;

            BaseEvent? evt = null;
            try
            {
                evt = JsonSerializer.Deserialize<BaseEvent>(json, s_options);
            }
            catch (JsonException)
            {
                // Malformed event — skip, do not throw (resilient parser)
                continue;
            }

            if (evt is not null) yield return evt;
        }
    }
}