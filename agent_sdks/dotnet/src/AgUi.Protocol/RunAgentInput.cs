using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgUi.Protocol;

/// <summary>
/// POST body for AG-UI HTTP/SSE endpoint.
/// Also used as the gRPC request message.
/// </summary>
public sealed record RunAgentInput
{
    [JsonPropertyName("threadId")]      public required string        ThreadId       { get; init; }
    [JsonPropertyName("runId")]         public required string        RunId          { get; init; }
    [JsonPropertyName("messages")]      public required JsonElement[] Messages       { get; init; }
    [JsonPropertyName("state")]         public JsonElement?           State          { get; init; }
    [JsonPropertyName("tools")]         public JsonElement[]?         Tools          { get; init; }
    [JsonPropertyName("context")]       public JsonElement[]?         Context        { get; init; }
    [JsonPropertyName("forwardedProps")] public JsonElement?          ForwardedProps { get; init; }
}