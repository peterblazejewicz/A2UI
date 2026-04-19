using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol;

/// <summary>
/// POST body for AG-UI HTTP/SSE endpoint.
/// Also used as the gRPC request message.
/// </summary>
public sealed record RunAgentInput
{
    /// <summary>Conversation thread identifier.</summary>
    [JsonPropertyName("threadId")]
    public required string ThreadId { get; init; }

    /// <summary>Unique identifier for this run.</summary>
    [JsonPropertyName("runId")]
    public required string RunId { get; init; }

    /// <summary>
    /// ID of the run that spawned this run; <see langword="null"/> for top-level runs.
    /// </summary>
    [JsonPropertyName("parentRunId")]
    public string? ParentRunId { get; init; }

    /// <summary>Conversation messages to send to the agent.</summary>
    [JsonPropertyName("messages")]
    public required JsonElement[] Messages { get; init; }

    /// <summary>Optional current state to restore on the agent.</summary>
    [JsonPropertyName("state")]
    public JsonElement? State { get; init; }

    /// <summary>Optional tool definitions available to the agent.</summary>
    [JsonPropertyName("tools")]
    public JsonElement[]? Tools { get; init; }

    /// <summary>Optional context elements for the agent.</summary>
    [JsonPropertyName("context")]
    public JsonElement[]? Context { get; init; }

    /// <summary>Optional properties forwarded to the agent from the client.</summary>
    [JsonPropertyName("forwardedProps")]
    public JsonElement? ForwardedProps { get; init; }
}
