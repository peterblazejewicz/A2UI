using System.Text.Json;
using System.Text.Json.Serialization;
using A2Ui.Core.Bindings;
using A2Ui.Core.Components.Extensions;

namespace A2Ui.Core.Components;

/// <summary>
/// A2UI v0.9 component in the flat adjacency list.
/// The agent may only reference component types registered in the catalog.
/// </summary>
/// <remarks>
/// The <see cref="JsonDerivedTypeAttribute"/> list below is split into two sections:
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Spec-core types</b> (Text through Slider) correspond to entries in
///       <c>specification/v0_9/json/basic_catalog.json</c> and live under
///       <see cref="Components"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Extensions</b> (Table, Surface) are project-local additions not present in
///       the v0.9 basic catalog. They live under <see cref="Components.Extensions"/> so a
///       future spec audit can distinguish spec types from extensions at a glance.
///     </description>
///   </item>
/// </list>
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "component")]
// ── Spec-core (v0.9 basic catalog) ─────────────────────────────────────
[JsonDerivedType(typeof(TextComponent), "Text")]
[JsonDerivedType(typeof(ButtonComponent), "Button")]
[JsonDerivedType(typeof(ColumnComponent), "Column")]
[JsonDerivedType(typeof(RowComponent), "Row")]
[JsonDerivedType(typeof(CardComponent), "Card")]
[JsonDerivedType(typeof(IconComponent), "Icon")]
[JsonDerivedType(typeof(DividerComponent), "Divider")]
[JsonDerivedType(typeof(ImageComponent), "Image")]
[JsonDerivedType(typeof(VideoComponent), "Video")]
[JsonDerivedType(typeof(AudioPlayerComponent), "AudioPlayer")]
[JsonDerivedType(typeof(ListComponent), "List")]
[JsonDerivedType(typeof(TabsComponent), "Tabs")]
[JsonDerivedType(typeof(ModalComponent), "Modal")]
[JsonDerivedType(typeof(TextFieldComponent), "TextField")]
[JsonDerivedType(typeof(DateTimeInputComponent), "DateTimeInput")]
[JsonDerivedType(typeof(ChoicePickerComponent), "ChoicePicker")]
[JsonDerivedType(typeof(CheckBoxComponent), "CheckBox")]
[JsonDerivedType(typeof(SliderComponent), "Slider")]
// ── Project-local extensions (not in v0.9 spec) ────────────────────────
[JsonDerivedType(typeof(TableComponent), "Table")]
[JsonDerivedType(typeof(SurfaceComponent), "Surface")]
public abstract record A2UiComponent
{
    // ── Identity ──────────────────────────────────────────────────────────

    /// <summary>Unique component identifier within the surface.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Component type discriminator. Populated by each sealed subtype's default value.
    /// Ignored by JSON serialization — the <c>[JsonPolymorphic]</c> discriminator handles
    /// the wire-format <c>"component"</c> field.
    /// </summary>
    [JsonIgnore]
    public virtual string Component { get; init; } = "";

    // ── Tree structure ────────────────────────────────────────────────────
    /// <summary>Legacy back-reference. v0.9 prefers forward-ref via children/child.</summary>
    [JsonPropertyName("parent")]
    public string? Parent { get; init; }

    /// <summary>Single child component ID (Card, Button, Modal trigger/content).</summary>
    [JsonPropertyName("child")]
    public string? Child { get; init; }

    /// <summary>v0.9 forward-reference children list (static IDs or template).</summary>
    [JsonPropertyName("children")]
    public ChildList? Children { get; init; }

    // ── ComponentCommon (common_types.json) ────────────────────────────────

    /// <summary>Accessibility attributes for screen readers.</summary>
    [JsonPropertyName("accessibility")]
    public AccessibilityAttributes? Accessibility { get; init; }

    /// <summary>Layout weight for proportional sizing within a container.</summary>
    [JsonPropertyName("weight")]
    public double? Weight { get; init; }

    // ── Checkable ─────────────────────────────────────────────────────────

    /// <summary>Client-side validation rules for this component.</summary>
    [JsonPropertyName("checks")]
    public CheckRule[]? Checks { get; init; }

    // ── Display properties ────────────────────────────────────────────────

    /// <summary>Primary text content of the component.</summary>
    [JsonPropertyName("text")]
    public DynamicValue? Text { get; init; }

    /// <summary>Label text displayed alongside or above the component.</summary>
    [JsonPropertyName("label")]
    public DynamicValue? Label { get; init; }

    // ── Extension data ────────────────────────────────────────────────────

    /// <summary>Captures any unrecognized JSON properties for forward compatibility.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; init; }
}
