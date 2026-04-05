using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Ui.Core.Messages;

/// <summary>
/// A2UI v0.9 component in the flat adjacency list.
/// The agent may only reference component types registered in the catalog.
/// </summary>
public sealed record A2UiComponent
{
    // ── Identity ──────────────────────────────────────────────────────────
    [JsonPropertyName("id")]        public required string   Id        { get; init; }
    [JsonPropertyName("component")] public required string   Component { get; init; }

    // ── Tree structure ────────────────────────────────────────────────────
    /// <summary>Legacy back-reference. v0.9 prefers forward-ref via children/child.</summary>
    [JsonPropertyName("parent")]   public string?    Parent   { get; init; }
    /// <summary>Single child component ID (Card, Button, Modal trigger/content).</summary>
    [JsonPropertyName("child")]    public string?    Child    { get; init; }
    /// <summary>v0.9 forward-reference children list (static IDs or template).</summary>
    [JsonPropertyName("children")] public ChildList? Children { get; init; }

    // ── ComponentCommon (common_types.json) ────────────────────────────────
    [JsonPropertyName("accessibility")] public AccessibilityAttributes? Accessibility { get; init; }
    [JsonPropertyName("weight")]        public double?                  Weight        { get; init; }

    // ── Checkable ─────────────────────────────────────────────────────────
    [JsonPropertyName("checks")] public CheckRule[]? Checks { get; init; }

    // ── Display properties ────────────────────────────────────────────────
    [JsonPropertyName("text")]        public DynamicValue?    Text        { get; init; }
    [JsonPropertyName("label")]       public DynamicValue?    Label       { get; init; }
    [JsonPropertyName("description")] public DynamicValue?    Description { get; init; }
    [JsonPropertyName("variant")]     public string?          Variant     { get; init; }
    [JsonPropertyName("value")]       public DynamicValue?    Value       { get; init; }
    [JsonPropertyName("url")]         public DynamicValue?    Url         { get; init; }
    [JsonPropertyName("action")]      public ComponentAction? Action      { get; init; }

    // ── Layout (Row, Column, List) ────────────────────────────────────────
    [JsonPropertyName("justify")]   public string? Justify   { get; init; }
    [JsonPropertyName("align")]     public string? Align     { get; init; }
    [JsonPropertyName("direction")] public string? Direction { get; init; }

    // ── Image ─────────────────────────────────────────────────────────────
    [JsonPropertyName("fit")] public string? Fit { get; init; }

    // ── Icon ──────────────────────────────────────────────────────────────
    [JsonPropertyName("name")] public DynamicValue? Name { get; init; }

    // ── Input (TextField, Slider, DateTimeInput) ──────────────────────────
    [JsonPropertyName("min")]              public DynamicValue? Min              { get; init; }
    [JsonPropertyName("max")]              public DynamicValue? Max              { get; init; }
    [JsonPropertyName("validationRegexp")] public string?       ValidationRegexp { get; init; }

    // ── DateTimeInput ─────────────────────────────────────────────────────
    [JsonPropertyName("enableDate")] public bool? EnableDate { get; init; }
    [JsonPropertyName("enableTime")] public bool? EnableTime { get; init; }

    // ── ChoicePicker ──────────────────────────────────────────────────────
    [JsonPropertyName("options")]      public ChoiceOption[]? Options      { get; init; }
    [JsonPropertyName("displayStyle")] public string?         DisplayStyle { get; init; }
    [JsonPropertyName("filterable")]   public bool?           Filterable   { get; init; }

    // ── Tabs ──────────────────────────────────────────────────────────────
    [JsonPropertyName("tabs")] public TabDefinition[]? Tabs { get; init; }

    // ── Modal ─────────────────────────────────────────────────────────────
    [JsonPropertyName("trigger")] public string? Trigger { get; init; }
    [JsonPropertyName("content")] public string? Content { get; init; }

    // ── Divider ───────────────────────────────────────────────────────────
    [JsonPropertyName("axis")] public string? Axis { get; init; }

    // ── Table (extension, not in v0.9 spec) ───────────────────────────────
    [JsonPropertyName("columns")] public TableColumn[]? Columns { get; init; }
    [JsonPropertyName("rows")]    public DynamicValue?   Rows    { get; init; }

    // ── Extension data ────────────────────────────────────────────────────
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}

// ── Supporting types ──────────────────────────────────────────────────────

/// <summary>Accessibility attributes for screen readers.</summary>
public sealed record AccessibilityAttributes
{
    [JsonPropertyName("label")]       public DynamicValue? Label       { get; init; }
    [JsonPropertyName("description")] public DynamicValue? Description { get; init; }
}

/// <summary>Client-side validation rule.</summary>
public sealed record CheckRule
{
    [JsonPropertyName("condition")] public required DynamicValue Condition { get; init; }
    [JsonPropertyName("message")]   public required string       Message   { get; init; }
}

/// <summary>Option for ChoicePicker component.</summary>
public sealed record ChoiceOption
{
    [JsonPropertyName("label")] public required string Label { get; init; }
    [JsonPropertyName("value")] public required string Value { get; init; }
}

/// <summary>Tab definition for Tabs component.</summary>
public sealed record TabDefinition
{
    [JsonPropertyName("title")] public required string Title { get; init; }
    [JsonPropertyName("child")] public required string Child { get; init; }
}

/// <summary>Action: server event or client function call.</summary>
public sealed record ComponentAction
{
    [JsonPropertyName("event")]        public ActionEvent?      Event        { get; init; }
    [JsonPropertyName("functionCall")] public FunctionCallValue? FunctionCall { get; init; }
}

/// <summary>Server-side event triggered by user interaction.</summary>
public sealed record ActionEvent
{
    [JsonPropertyName("name")]    public required string Name { get; init; }
    [JsonPropertyName("context")] public Dictionary<string, DynamicValue>? Context { get; init; }
}

/// <summary>Table column definition (extension, not in v0.9 spec).</summary>
public sealed record TableColumn
{
    [JsonPropertyName("header")] public required string Header { get; init; }
    [JsonPropertyName("field")]  public required string Field  { get; init; }
}
