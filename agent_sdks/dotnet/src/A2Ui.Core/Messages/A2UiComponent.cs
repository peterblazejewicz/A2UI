using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// A2UI component in the flat adjacency list.
/// The agent may only reference component types registered in the catalog.
/// </summary>
public sealed record A2UiComponent
{
    /// <summary>Unique ID within surface. String, agent-assigned.</summary>
    [JsonPropertyName("id")]        public required string   Id        { get; init; }

    /// <summary>Type key — must exist in the client catalog.</summary>
    [JsonPropertyName("component")] public required string   Component { get; init; }

    /// <summary>Parent component ID for tree structure. Null = root.</summary>
    [JsonPropertyName("parent")]    public string?           Parent    { get; init; }

    /// <summary>Single child component ID (for components like Button).</summary>
    [JsonPropertyName("child")]     public string?           Child     { get; init; }

    // ── Common display properties ──────────────────────────────────────────
    [JsonPropertyName("text")]      public BoundOrLiteral?  Text      { get; init; }
    [JsonPropertyName("label")]     public string?          Label     { get; init; }
    [JsonPropertyName("variant")]   public string?          Variant   { get; init; }
    [JsonPropertyName("value")]     public BoundOrLiteral?  Value     { get; init; }
    [JsonPropertyName("url")]       public string?          Url       { get; init; }
    [JsonPropertyName("action")]    public ComponentAction? Action    { get; init; }

    // ── DateTimeInput specific ─────────────────────────────────────────────
    [JsonPropertyName("enableDate")] public bool? EnableDate { get; init; }
    [JsonPropertyName("enableTime")] public bool? EnableTime { get; init; }

    // ── Table specific ────────────────────────────────────────────────────
    [JsonPropertyName("columns")]   public TableColumn[]?   Columns   { get; init; }
    [JsonPropertyName("rows")]      public BoundOrLiteral?  Rows      { get; init; }

    // ── Extension — additional arbitrary properties ────────────────────────
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

/// <summary>
/// Either a literal value (string/number/bool) or a BoundValue path.
/// On the wire: <c>{"path": "/reservation/date"}</c> for bound,
/// or a plain string <c>"Hello"</c> for literal.
/// </summary>
[JsonConverter(typeof(BoundOrLiteralConverter))]
public sealed record BoundOrLiteral
{
    public string?  Literal { get; init; }
    public string?  Path    { get; init; }   // JSON Pointer path

    public bool IsBound => Path is not null;

    public static BoundOrLiteral Literal_(string value) => new() { Literal = value };
    public static BoundOrLiteral Bound_(string path)    => new() { Path = path };
}

public sealed record ComponentAction
{
    [JsonPropertyName("event")] public ActionEvent? Event { get; init; }
}

public sealed record TableColumn
{
    [JsonPropertyName("header")] public required string Header  { get; init; }
    [JsonPropertyName("field")]  public required string Field   { get; init; }
}

/// <summary>
/// Converter for BoundOrLiteral to handle the union of literal and bound value.
/// </summary>
internal sealed class BoundOrLiteralConverter : JsonConverter<BoundOrLiteral>
{
    public override BoundOrLiteral? Read(ref Utf8JsonReader reader,
        Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return BoundOrLiteral.Literal_(reader.GetString()!);

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            if (doc.RootElement.TryGetProperty("path", out var pathEl))
                return BoundOrLiteral.Bound_(pathEl.GetString()!);
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, BoundOrLiteral value,
        JsonSerializerOptions options)
    {
        if (value.IsBound)
        {
            writer.WriteStartObject();
            writer.WriteString("path", value.Path);
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteStringValue(value.Literal);
        }
    }
}