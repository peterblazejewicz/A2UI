using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// A2UI v0.9 client-to-server message envelope.
/// Contains exactly one of: action or error.
/// </summary>
public sealed record ClientToServerMessage
{
    [JsonPropertyName("version")] public string Version { get; init; } = "v0.9";
    [JsonPropertyName("action")]  public ClientAction? Action { get; init; }
    [JsonPropertyName("error")]   public ClientError?  Error  { get; init; }
}

/// <summary>
/// Client-to-server action — triggered by user interaction with a component.
/// </summary>
public sealed record ClientAction
{
    [JsonPropertyName("name")]              public required string     Name              { get; init; }
    [JsonPropertyName("surfaceId")]         public required string     SurfaceId         { get; init; }
    [JsonPropertyName("sourceComponentId")] public required string     SourceComponentId { get; init; }
    [JsonPropertyName("timestamp")]         public required string     Timestamp         { get; init; }
    [JsonPropertyName("context")]           public required JsonElement Context          { get; init; }
}

/// <summary>
/// Client-to-server error — validation failure or generic error.
/// </summary>
public sealed record ClientError
{
    [JsonPropertyName("code")]      public required string Code      { get; init; }
    [JsonPropertyName("surfaceId")] public required string SurfaceId { get; init; }
    [JsonPropertyName("message")]   public required string Message   { get; init; }

    /// <summary>JSON Pointer to the failed field. Only for VALIDATION_FAILED code.</summary>
    [JsonPropertyName("path")]      public string?         Path      { get; init; }
}
