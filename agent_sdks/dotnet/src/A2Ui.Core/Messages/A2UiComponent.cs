using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// A2UI component in the flat adjacency list.
/// The agent may only reference component types registered in the catalog.
/// </summary>
public sealed record A2UiComponent
{
    [JsonPropertyName("id")]        public required string   Id        { get; init; }
    [JsonPropertyName("component")] public required string   Component { get; init; }

    /// <summary>Parent component ID for tree structure. Null = root. Legacy back-ref.</summary>
    [JsonPropertyName("parent")]    public string?           Parent    { get; init; }

    /// <summary>Single child component ID (for Card, Button, etc.).</summary>
    [JsonPropertyName("child")]     public string?           Child     { get; init; }

    // ── Common display properties ──────────────────────────────────────────
    [JsonPropertyName("text")]      public DynamicValue?     Text      { get; init; }
    [JsonPropertyName("label")]     public DynamicValue?     Label     { get; init; }
    [JsonPropertyName("variant")]   public string?           Variant   { get; init; }
    [JsonPropertyName("value")]     public DynamicValue?     Value     { get; init; }
    [JsonPropertyName("url")]       public DynamicValue?     Url       { get; init; }
    [JsonPropertyName("action")]    public ComponentAction?  Action    { get; init; }

    // ── DateTimeInput specific ─────────────────────────────────────────────
    [JsonPropertyName("enableDate")] public bool? EnableDate { get; init; }
    [JsonPropertyName("enableTime")] public bool? EnableTime { get; init; }

    // ── Table specific (extension, not in v0.9 spec) ──────────────────────
    [JsonPropertyName("columns")]   public TableColumn[]?    Columns   { get; init; }
    [JsonPropertyName("rows")]      public DynamicValue?     Rows      { get; init; }

    // ── Extension — additional arbitrary properties ────────────────────────
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

public sealed record ComponentAction
{
    [JsonPropertyName("event")]        public ActionEvent?     Event        { get; init; }
    [JsonPropertyName("functionCall")] public FunctionCallValue? FunctionCall { get; init; }
}

public sealed record ActionEvent
{
    [JsonPropertyName("name")]    public required string Name { get; init; }

    /// <summary>Key-value context pairs. Values can be DynamicValue (resolved at send time).</summary>
    [JsonPropertyName("context")] public Dictionary<string, DynamicValue>? Context { get; init; }
}

public sealed record TableColumn
{
    [JsonPropertyName("header")] public required string Header  { get; init; }
    [JsonPropertyName("field")]  public required string Field   { get; init; }
}
