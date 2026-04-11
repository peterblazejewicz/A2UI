using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using A2Ui.Avalonia.Shell.Models;
using A2Ui.Core.Messages;
using Microsoft.Extensions.Logging;

namespace A2Ui.Avalonia.Shell.Services;

/// <summary>
/// A2A HTTP client that communicates with a Python (or .NET) agent supporting A2UI.
/// Port of <c>samples/client/lit/shell/client.ts</c>.
/// </summary>
public sealed class A2AAgentClient : IA2AClient
{
    private const string A2UiMimeType = "application/json+a2ui";

    /// <summary>A2A extension negotiation header name. Used by DI at client registration time.</summary>
    internal const string A2UiExtensionHeader = "X-A2A-Extensions";

    /// <summary>A2UI v0.9 extension URI. Used by DI at client registration time.</summary>
    internal const string A2UiExtensionUri = "https://a2ui.org/a2a-extension/a2ui/v0.9";

    private static readonly JsonSerializerOptions s_camelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly AgentConfig _config;
    private readonly ILogger<A2AAgentClient> _logger;

    public A2AAgentClient(HttpClient http, AgentConfig config, ILogger<A2AAgentClient> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<string> GetAgentNameAsync(CancellationToken ct = default)
    {
        string cardUrl = $"{_config.ServerUrl}/.well-known/agent-card.json";
        A2AAgentClientLog.FetchingAgentCard(_logger, cardUrl);

        A2AAgentCard? card = await _http.GetFromJsonAsync<A2AAgentCard>(cardUrl, s_camelCase, ct).ConfigureAwait(false);

        return card?.Name ?? "Unknown Agent";
    }

    public async Task<IReadOnlyList<A2UiMessage>> SendTextAsync(string text, CancellationToken ct = default)
    {
        A2AAgentClientLog.SendingTextQuery(_logger, Truncate(text, 200));

        var parts = new A2APart[]
        {
            new A2ATextPart { Kind = "text", Text = text },
        };

        return await SendAsync(parts, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<A2UiMessage>> SendActionAsync(object userAction, CancellationToken ct = default)
    {
        A2AAgentClientLog.SendingAction(_logger);

        JsonElement data = JsonSerializer.SerializeToElement(userAction, s_camelCase);
        var parts = new A2APart[]
        {
            new A2ADataPart
            {
                Kind = "data",
                Data = data,
                MimeType = A2UiMimeType,
            },
        };

        return await SendAsync(parts, ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<A2UiMessage>> SendAsync(A2APart[] parts, CancellationToken ct)
    {
        string correlationId = Guid.NewGuid().ToString("N");
        string messageId = Guid.NewGuid().ToString();
        var request = new A2ASendMessageRequest
        {
            Message = new A2AMessage { MessageId = messageId, Parts = parts },
        };

        // Serialize once to get the byte count for telemetry, then POST the same bytes.
        byte[] requestBytes = JsonSerializer.SerializeToUtf8Bytes(request, s_camelCase);
        A2AAgentClientLog.SendMessageStarted(_logger, _config.ServerUrl, requestBytes.Length, messageId, correlationId);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var content = new ByteArrayContent(requestBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpResponseMessage response = await _http.PostAsync(_config.ServerUrl, content, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            byte[] responseBodyBytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            A2ATaskResponse? taskResponse = JsonSerializer.Deserialize<A2ATaskResponse>(responseBodyBytes, s_camelCase);

            IReadOnlyList<A2UiMessage> messages = ExtractA2UiMessages(taskResponse, correlationId);

            stopwatch.Stop();
            A2AAgentClientLog.SendMessageCompleted(
                _logger,
                (int)response.StatusCode,
                responseBodyBytes.LongLength,
                messages.Count,
                stopwatch.ElapsedMilliseconds,
                correlationId
            );

            return messages;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            A2AAgentClientLog.SendMessageFailed(
                _logger,
                _config.ServerUrl,
                stopwatch.ElapsedMilliseconds,
                ex.GetType().Name,
                correlationId,
                ex
            );
            throw;
        }
    }

    private List<A2UiMessage> ExtractA2UiMessages(A2ATaskResponse? taskResponse, string correlationId)
    {
        var messages = new List<A2UiMessage>();

        A2AResponsePart[]? parts = taskResponse?.Result?.Status?.Message?.Parts;
        if (parts is null)
        {
            A2AAgentClientLog.ResponseHasNoParts(_logger, correlationId);
            return messages;
        }

        int messageIndex = 0;
        foreach (A2AResponsePart part in parts)
        {
            int currentIndex = messageIndex++;

            if (part.Kind != "data" || part.Data is null)
                continue;

            // Check mimeType in the part itself or in metadata
            string? mime = part.MimeType ?? GetMimeFromMetadata(part.Metadata);
            if (mime is not null && mime != A2UiMimeType)
                continue;

            try
            {
                A2UiMessage? msg = part.Data.Value.Deserialize<A2UiMessage>(s_camelCase);
                if (msg is not null)
                    messages.Add(msg);
            }
            catch (JsonException ex)
            {
                A2AAgentClientLog.DataPartDeserializeFailed(_logger, currentIndex, correlationId, ex);
            }
        }

        A2AAgentClientLog.ExtractedMessages(_logger, messages.Count, correlationId);
        return messages;
    }

    private static string? GetMimeFromMetadata(JsonElement? metadata)
    {
        if (metadata is null)
            return null;

        if (
            metadata.Value.ValueKind == JsonValueKind.Object
            && metadata.Value.TryGetProperty("mimeType", out JsonElement mimeElement)
        )
        {
            return mimeElement.GetString();
        }

        return null;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}

internal static partial class A2AAgentClientLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Fetching agent card from {Url}")]
    public static partial void FetchingAgentCard(ILogger logger, string url);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Sending text query: {TextPreview}")]
    public static partial void SendingTextQuery(ILogger logger, string textPreview);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Sending action to agent")]
    public static partial void SendingAction(ILogger logger);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "No parts in agent response (correlationId={CorrelationId})"
    )]
    public static partial void ResponseHasNoParts(ILogger logger, string correlationId);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Failed to deserialize A2UI message from DataPart (messageIndex={MessageIndex}, correlationId={CorrelationId})"
    )]
    public static partial void DataPartDeserializeFailed(
        ILogger logger,
        int messageIndex,
        string correlationId,
        Exception exception
    );

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "Extracted {MessageCount} A2UI message(s) (correlationId={CorrelationId})"
    )]
    public static partial void ExtractedMessages(ILogger logger, int messageCount, string correlationId);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "SendMessage started: url={HttpUrl}, requestBytes={RequestBytes}, messageId={MessageId}, correlationId={CorrelationId}"
    )]
    public static partial void SendMessageStarted(
        ILogger logger,
        string httpUrl,
        int requestBytes,
        string messageId,
        string correlationId
    );

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Information,
        Message = "SendMessage completed: httpStatus={HttpStatus}, responseBytes={ResponseBytes}, messageCount={MessageCount}, durationMs={DurationMs}, correlationId={CorrelationId}"
    )]
    public static partial void SendMessageCompleted(
        ILogger logger,
        int httpStatus,
        long responseBytes,
        int messageCount,
        long durationMs,
        string correlationId
    );

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Error,
        Message = "SendMessage failed: url={HttpUrl}, durationMs={DurationMs}, exceptionType={ExceptionType}, correlationId={CorrelationId}"
    )]
    public static partial void SendMessageFailed(
        ILogger logger,
        string httpUrl,
        long durationMs,
        string exceptionType,
        string correlationId,
        Exception exception
    );
}
