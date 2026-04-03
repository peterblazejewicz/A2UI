using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// A2UI v0.8/v0.9 message — one JSONL line on the wire.
/// Each message contains exactly one top-level operation key.
/// </summary>
public sealed record A2UiMessage
{
    [JsonPropertyName("version")]          public string?              Version          { get; init; }
    [JsonPropertyName("createSurface")]    public CreateSurface?       CreateSurface    { get; init; }
    [JsonPropertyName("deleteSurface")]    public DeleteSurface?       DeleteSurface    { get; init; }
    [JsonPropertyName("updateComponents")] public UpdateComponents?    UpdateComponents { get; init; }
    [JsonPropertyName("updateDataModel")]  public UpdateDataModel?     UpdateDataModel  { get; init; }
    [JsonPropertyName("userAction")]       public UserAction?          UserAction       { get; init; }
    [JsonPropertyName("dataModelUpdate")]  public DataModelUpdate?     DataModelUpdate  { get; init; }
}

public sealed record CreateSurface
{
    [JsonPropertyName("surfaceId")]  public required string SurfaceId  { get; init; }
    [JsonPropertyName("catalogId")]  public required string CatalogId  { get; init; }
}

public sealed record DeleteSurface
{
    [JsonPropertyName("surfaceId")]  public required string SurfaceId { get; init; }
}

public sealed record UpdateComponents
{
    [JsonPropertyName("surfaceId")]   public required string       SurfaceId  { get; init; }
    [JsonPropertyName("components")]  public required A2UiComponent[] Components { get; init; }
}

/// <summary>Updates the data model for BoundValue resolution.</summary>
public sealed record UpdateDataModel
{
    [JsonPropertyName("surfaceId")] public required string      SurfaceId { get; init; }
    [JsonPropertyName("path")]      public required string      Path      { get; init; }
    [JsonPropertyName("value")]     public required JsonElement Value     { get; init; }
}

/// <summary>v0.8 data model update with key-value map structure.</summary>
public sealed record DataModelUpdate
{
    [JsonPropertyName("surfaceId")] public required string             SurfaceId { get; init; }
    [JsonPropertyName("contents")]  public required DataModelContent[] Contents  { get; init; }
}

public sealed record DataModelContent
{
    [JsonPropertyName("key")]       public required string             Key      { get; init; }
    [JsonPropertyName("valueString")] public string?                  ValueString { get; init; }
    [JsonPropertyName("valueInt")]    public int?                     ValueInt    { get; init; }
    [JsonPropertyName("valueBool")]   public bool?                    ValueBool   { get; init; }
    [JsonPropertyName("valueMap")]    public DataModelContent[]?      ValueMap    { get; init; }
}

/// <summary>Client→agent: user interaction event.</summary>
public sealed record UserAction
{
    [JsonPropertyName("surfaceId")] public required string      SurfaceId { get; init; }
    [JsonPropertyName("event")]     public required ActionEvent Event     { get; init; }
}

public sealed record ActionEvent
{
    [JsonPropertyName("name")]    public required string Name    { get; init; }
    [JsonPropertyName("payload")] public JsonElement? Payload    { get; init; }
}