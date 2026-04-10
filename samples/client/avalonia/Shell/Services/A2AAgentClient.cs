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
    private const string A2UiExtensionHeader = "X-A2A-Extensions";
    private const string A2UiExtensionUri = "https://a2ui.org/a2a-extension/a2ui/v0.9";

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

        _http.DefaultRequestHeaders.TryAddWithoutValidation(A2UiExtensionHeader, A2UiExtensionUri);
    }

    public async Task<string> GetAgentNameAsync(CancellationToken ct = default)
    {
        string cardUrl = $"{_config.ServerUrl}/.well-known/agent-card.json";
        _logger.LogDebug("Fetching agent card from {Url}", cardUrl);

        A2AAgentCard? card = await _http.GetFromJsonAsync<A2AAgentCard>(cardUrl, s_camelCase, ct).ConfigureAwait(false);

        return card?.Name ?? "Unknown Agent";
    }

    public async Task<IReadOnlyList<A2UiMessage>> SendTextAsync(string text, CancellationToken ct = default)
    {
        _logger.LogDebug("Sending text query: {Text}", text);

        var parts = new A2APart[]
        {
            new A2ATextPart { Kind = "text", Text = text },
        };

        return await SendAsync(parts, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<A2UiMessage>> SendActionAsync(object userAction, CancellationToken ct = default)
    {
        _logger.LogDebug("Sending action to agent");

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
        var request = new A2ASendMessageRequest
        {
            Message = new A2AMessage { MessageId = Guid.NewGuid().ToString(), Parts = parts },
        };

        HttpResponseMessage response = await _http
            .PostAsJsonAsync(_config.ServerUrl, request, s_camelCase, ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        A2ATaskResponse? taskResponse = await response
            .Content.ReadFromJsonAsync<A2ATaskResponse>(s_camelCase, ct)
            .ConfigureAwait(false);

        return ExtractA2UiMessages(taskResponse);
    }

    private List<A2UiMessage> ExtractA2UiMessages(A2ATaskResponse? taskResponse)
    {
        var messages = new List<A2UiMessage>();

        A2AResponsePart[]? parts = taskResponse?.Result?.Status?.Message?.Parts;
        if (parts is null)
        {
            _logger.LogWarning("No parts in agent response");
            return messages;
        }

        foreach (A2AResponsePart part in parts)
        {
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
                _logger.LogWarning(ex, "Failed to deserialize A2UI message from DataPart");
            }
        }

        _logger.LogDebug("Extracted {Count} A2UI message(s) from response", messages.Count);
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
}
