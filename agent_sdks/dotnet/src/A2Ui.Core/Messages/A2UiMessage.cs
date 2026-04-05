using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// A2UI v0.9 server-to-client message — one JSONL line on the wire.
/// Each message contains exactly one top-level operation key.
/// </summary>
public sealed record A2UiMessage
{
    [JsonPropertyName("version")]          public string?           Version          { get; init; }
    [JsonPropertyName("createSurface")]    public CreateSurface?    CreateSurface    { get; init; }
    [JsonPropertyName("deleteSurface")]    public DeleteSurface?    DeleteSurface    { get; init; }
    [JsonPropertyName("updateComponents")] public UpdateComponents? UpdateComponents { get; init; }
    [JsonPropertyName("updateDataModel")]  public UpdateDataModel?  UpdateDataModel  { get; init; }
}

public sealed record CreateSurface
{
    [JsonPropertyName("surfaceId")]     public required string SurfaceId     { get; init; }
    [JsonPropertyName("catalogId")]     public required string CatalogId     { get; init; }
    [JsonPropertyName("theme")]         public JsonElement?    Theme         { get; init; }
    [JsonPropertyName("sendDataModel")] public bool?           SendDataModel { get; init; }
}

public sealed record DeleteSurface
{
    [JsonPropertyName("surfaceId")] public required string SurfaceId { get; init; }
}

public sealed record UpdateComponents
{
    [JsonPropertyName("surfaceId")]  public required string          SurfaceId  { get; init; }
    [JsonPropertyName("components")] public required A2UiComponent[] Components { get; init; }
}

/// <summary>
/// Updates the data model at a JSON Pointer path.
/// If path is null or "/", replaces the entire model.
/// If value is null, deletes the key at path.
/// </summary>
public sealed record UpdateDataModel
{
    [JsonPropertyName("surfaceId")] public required string  SurfaceId { get; init; }
    [JsonPropertyName("path")]      public string?          Path      { get; init; }
    [JsonPropertyName("value")]     public JsonElement?     Value     { get; init; }
}
