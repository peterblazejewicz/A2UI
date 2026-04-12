# Type Modeling Elegance — Design Spec

**Date:** 2026-04-12
**Status:** Approved
**Scope:** AgUi.Protocol, A2Ui.Core, A2Ui.Avalonia, all test projects
**Goal:** NuGet-ready type elegance for A2Ui.Core; typed contracts familiar to .NET practitioners

---

## Motivation

A comparative review against `neuroglia-io/a2ui` (`a2ui-net`) revealed two
genuinely better type-modeling ideas and four improvement opportunities:

1. **Discriminated unions** for dynamic values — `a2ui-net` uses `OneOf<T1,T2,T3>`
   with `Match`/`Switch` forcing exhaustive handling. Our `DynamicValue` is a flat
   record with 6 optional nullable properties and implicit priority ordering. Nothing
   prevents setting both `Path` and `StringLiteral` simultaneously.

2. **Typed per-component records** — `a2ui-net` gives each component its own
   `sealed record` with only its relevant fields. Our `A2UiComponent` is a single
   record with ~30 optional properties; a `TextField` can accidentally have `Tabs`
   set.

3. **EventType enum drift** — the `EventType` enum and `[JsonDerivedType]` attributes
   on `BaseEvent` are two parallel lists with no compiler or test guard ensuring they
   stay in sync.

4. **FormatString stub** — `BuiltInFunctions.FormatString` is registered in the
   `FunctionRegistry` but returns its input as-is. The real implementation lives in
   `RenderContext.ResolveFormatString()`. Direct registry callers get wrong results.

5. **ICatalogEntry.Update** — 10 of 20 catalog entries always return `false`,
   forcing the renderer to recreate controls on every `updateComponents` message.

6. **XML documentation** — public API types lack the documentation expected of a
   NuGet-published library.

If `A2Ui.Core` is published to NuGet, the flat `DynamicValue` and untyped
`A2UiComponent` would confuse .NET practitioners accustomed to typed contracts.

---

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| DynamicValue modeling | Domain-specific sealed hierarchy with `Match<T>` | 6 variants including duplicate `string` (literal vs path) — generic `OneOf` is unwieldy |
| Component modeling | `[JsonPolymorphic]` typed all the way (no wire DTO) | v0.9 spec is stable; mapping layer is overhead |
| OneOf implementation | Hand-rolled in A2Ui.Core | Zero dependencies; full control; avoid `a2ui-net`'s naming bug |
| XML doc scope | Full NuGet-grade for Core; types+methods for Protocol and Avalonia | Core is the NuGet surface; others are internal consumers |
| Migration strategy | Two-phase typed components (polymorphism first, property migration second) | Zero-breakage Phase A; controlled blast radius Phase B |

---

## Units of Change

Each unit is independently buildable, testable, and code-reviewable.
Gate for every unit: `dotnet build A2Ui.slnx --configuration Release` zero
errors/warnings + `dotnet test A2Ui.slnx --configuration Release --no-build`
all passing.

### Dependency Graph

```
Unit 0 (FormatString) ──────────────► independent
Unit 1 (EventType test) ────────────► independent
Unit 2 (OneOf types) ───────────────► independent
Unit 3 (DynamicValue + compat) ─────► no dependency
Unit 4 (remove compat) ────────────── depends on: Unit 3
Unit 5 (ICatalogEntry.Update) ──────► independent
Unit 6 (typed components Phase A) ── depends on: Unit 3
Unit 7a-d (typed components Phase B) depends on: Unit 4, Unit 6
Unit 8 (XML docs) ─────────────────── depends on: Unit 7
```

---

### Unit 0 — FormatString Stub Correction

**Risk:** Minimal | **Files:** 2-3

The `formatString` function is registered in `FunctionRegistry.CreateDefault()` but
the registered stub (`BuiltInFunctions.FormatString`) returns args `"value"` as-is.
The real implementation is the special-case branch in
`RenderContext.ResolveFunction()` at `A2UiRenderer.cs:386` which delegates to
`ResolveFormatString()` → `ExpressionParser` → token resolution. The registry
entry is dead code that produces wrong output if called directly.

**Changes:**

- `FunctionRegistry.cs`: remove `.Register("formatString", BuiltInFunctions.FormatString)`
  from the `CreateDefault()` builder chain
- `BuiltInFunctions.cs`: remove the `FormatString` method entirely (or mark
  `[Obsolete("Use RenderContext for formatString evaluation", error: true)]`)
- Add test asserting `FunctionRegistry.CreateDefault().Evaluate("formatString", ...)`
  returns `null` (unknown function)

---

### Unit 1 — EventType Enum Drift Test

**Risk:** Minimal | **Files:** 1 new

**Changes:**

- New `EventTypeCoverageTests.cs` in `AgUi.Protocol.Tests/Events/`
- Reflects over `BaseEvent` to read all `[JsonDerivedType]` attributes
- Extracts discriminator strings (e.g., `"RUN_STARTED"`)
- Converts to PascalCase (strip underscores) and validates against
  `Enum.GetValues<EventType>()`
- Validates both directions: every discriminator has an enum value, every enum
  value has a discriminator

---

### Unit 2 — OneOf Generic Discriminated Unions

**Risk:** Low | **Files:** 2 new

New file `A2Ui.Core/Messages/OneOf.cs` with two generic types. These will be used
by typed component properties (e.g., `Text.Content` as
`OneOf<DataBinding, FunctionCall, string>`).

#### OneOf\<T1, T2\>

```csharp
[JsonConverter(typeof(OneOfConverterFactory))]  // if needed, or per-property
public abstract record OneOf<T1, T2>
{
    public sealed record Case1(T1 Value) : OneOf<T1, T2>;
    public sealed record Case2(T2 Value) : OneOf<T1, T2>;

    public bool IsT1 => this is Case1;
    public bool IsT2 => this is Case2;

    public T Match<T>(Func<T1, T> onT1, Func<T2, T> onT2);
    public void Switch(Action<T1> onT1, Action<T2> onT2);

    public bool TryGetAsT1(out T1? value);
    public bool TryGetAsT2(out T2? value);

    public static implicit operator OneOf<T1, T2>(T1 value) => new Case1(value);
    public static implicit operator OneOf<T1, T2>(T2 value) => new Case2(value);
}
```

#### OneOf\<T1, T2, T3\>

Same pattern with three cases. **Critical: `TryGetAsT3` must be correctly named**
(the `a2ui-net` implementation has a copy-paste bug naming it `TryGetAsT2`).

#### Tests

New `OneOfTests.cs` covering:

- Construction via implicit conversion
- `Match` returns correct value for each case
- `Switch` invokes correct action for each case
- `TryGetAsT1/T2/T3` returns true/false correctly
- `IsT1/IsT2/IsT3` computed properties
- Record equality (two `Case1("hello")` are equal)
- Exhaustiveness: `Match` with wrong case throws (unreachable, but verified)

---

### Unit 3 — DynamicValue Sealed Hierarchy (with compat properties)

**Risk:** Medium | **Files:** 2 modified, tests updated

This is the pivotal unit. `DynamicValue` transforms from a flat record to an
abstract sealed hierarchy while preserving backward compatibility.

#### Type Hierarchy

```csharp
[JsonConverter(typeof(DynamicValueConverter))]
public abstract record DynamicValue
{
    // --- Subtypes ---
    public sealed record StringValue(string Value) : DynamicValue;
    public sealed record NumberValue(double Value) : DynamicValue;
    public sealed record BoolValue(bool Value) : DynamicValue;
    public sealed record ArrayValue(JsonElement Value) : DynamicValue;
    public sealed record PathValue(string Path) : DynamicValue;
    public sealed record FunctionValue(FunctionCallValue Call) : DynamicValue;

    // --- Exhaustive match ---
    public abstract T Match<T>(
        Func<StringValue, T> onString,
        Func<NumberValue, T> onNumber,
        Func<BoolValue, T> onBool,
        Func<ArrayValue, T> onArray,
        Func<PathValue, T> onPath,
        Func<FunctionValue, T> onFunction);

    public abstract void Switch(
        Action<StringValue> onString,
        Action<NumberValue> onNumber,
        Action<BoolValue> onBool,
        Action<ArrayValue> onArray,
        Action<PathValue> onPath,
        Action<FunctionValue> onFunction);

    // --- Factory methods (preserved API) ---
    public static DynamicValue FromString(string value) => new StringValue(value);
    public static DynamicValue FromPath(string path) => new PathValue(path);
    public static DynamicValue FromNumber(double value) => new NumberValue(value);
    public static DynamicValue FromBool(bool value) => new BoolValue(value);

    // --- Backward-compat computed properties (removed in Unit 4) ---
    public string? StringLiteral => this is StringValue s ? s.Value : null;
    public double? NumberLiteral => this is NumberValue n ? n.Value : null;
    public bool? BoolLiteral => this is BoolValue b ? b.Value : null;
    public JsonElement? ArrayLiteral => this is ArrayValue a ? a.Value : null;
    public string? Path => this is PathValue p ? p.Path : null;
    public FunctionCallValue? FunctionCall => this is FunctionValue f ? f.Call : null;

    public bool IsBound => this is PathValue;
    public bool IsFunction => this is FunctionValue;
    public bool IsLiteral => this is not PathValue and not FunctionValue;
}
```

#### Why compat properties

The following consumers currently access flat properties and would break without
the compat layer:

- `DataModel.Resolve()` — chains if-checks on `StringLiteral`, `NumberLiteral`, etc.
- `A2UiRenderer.ResolveCore()` — checks `.FunctionCall`, `.ArrayLiteral`, `.Path`
- `InputCatalogEntries` — 4 occurrences of `.Value?.Path` for binding extraction
- `A2UiRenderer.ExpandTemplate()` — extracts `.Path`
- ~15 test files use `.StringLiteral`, `.Path`, `.IsFunction`, etc. in assertions

The compat properties allow **all 487 tests to pass unchanged** in this unit.

#### Converter update

`DynamicValueConverter.Read` already dispatches on `JsonTokenType`. Each branch
returns the concrete subtype:

- `JsonTokenType.String` → `new StringValue(reader.GetString()!)`
- `JsonTokenType.Number` → `new NumberValue(reader.GetDouble())`
- `JsonTokenType.True/False` → `new BoolValue(true/false)`
- `JsonTokenType.StartArray` → `new ArrayValue(JsonElement.ParseValue(ref reader))`
- `JsonTokenType.StartObject` with `"path"` → `new PathValue(path)`
- `JsonTokenType.StartObject` with `"call"` → `new FunctionValue(fc)`

`DynamicValueConverter.Write` becomes a `Match` dispatch.

#### DataModel.Resolve() update

```csharp
public string? Resolve(DynamicValue? value)
{
    if (value is null) return null;
    return value.Match<string?>(
        onString: s => s.Value,
        onNumber: n => n.Value.ToString(CultureInfo.InvariantCulture),
        onBool: b => b.Value ? "true" : "false",
        onArray: _ => null,
        onPath: p => ResolvePathAsString(p.Path),
        onFunction: _ => null);
}
```

#### New tests

- `Match<T>` exhaustiveness for each subtype
- Subtype identity: `DynamicValue.FromString("x") is DynamicValue.StringValue`
- Record equality: `new StringValue("a") == new StringValue("a")`
- All 14 existing `DynamicValueConverterTests` pass unchanged via compat properties

---

### Unit 4 — Remove Compat Accessors

**Risk:** Low | **Files:** 4-5 modified

After Unit 3 ships, remove the backward-compat properties from the abstract base.
The compiler will identify every remaining consumer. All changes are mechanical.

**Changes:**

| Consumer | Old pattern | New pattern |
|----------|------------|-------------|
| `A2UiRenderer.ResolveCore()` | `value.FunctionCall is { } fc` | `value.Match(...)` or `value is FunctionValue { Call: var fc }` |
| `A2UiRenderer.ResolveFormatString()` | `valDv?.StringLiteral` | `valDv is StringValue sv ? sv.Value : null` |
| `A2UiRenderer.ExpandTemplate()` | `tmpl.Path.TrimStart('/')` | `((PathValue)tmpl).Path.TrimStart('/')` (safe — template always has path) |
| `InputCatalogEntries` (4 sites) | `component.Value?.Path` | Helper: `DynamicValueExtensions.GetBindingPath(DynamicValue?)` |
| `DynamicValueConverterTests` | `.StringLiteral` assertions | `is StringValue { Value: "..." }` pattern match assertions |

---

### Unit 5 — ICatalogEntry.Update Improvements

**Risk:** Low-Medium | **Files:** 5 modified

#### Entries to implement Update on

| Entry | Update strategy |
|-------|----------------|
| `ButtonCatalogEntry` | Update Content (text/child), variant classes, IsEnabled (checks), ToolTip. Action handler closure is safe to retain — it resolves context at click time. |
| `CardCatalogEntry` | Single-child path: update `Border.Child` via `RenderChild`. Multi-child fallback: return `false`. |
| `ListCatalogEntry` | Direction guard (return `false` on change). Otherwise clear + re-add children. **Preserves ScrollViewer scroll position.** |
| `TabsCatalogEntry` | Tab count guard (return `false` on change). Otherwise update header + content per `TabItem`. **Preserves selected tab index.** |
| `TableCatalogEntry` | Column count guard. Update `DataGridTextColumn.Header` in-place. |
| `SurfaceCatalogEntry` | Clear + re-add children. Avoids full root tree detach/reattach. |
| `VideoCatalogEntry` | Update `TextBlock.Text` (placeholder). |
| `AudioPlayerCatalogEntry` | Update `TextBlock.Text` (placeholder). |

#### Entries to keep returning false (with documented rationale)

| Entry | Rationale |
|-------|-----------|
| `ColumnCatalogEntry` | Panel type (StackPanel vs Grid) determined at creation by justify/weights; cannot be mutated in-place. |
| `RowCatalogEntry` | Same as Column. |
| `ModalCatalogEntry` | `Popup.PlacementTarget` goes stale if trigger control is recreated; `AttachedToVisualTree`/`DetachedFromVisualTree` lifecycle handlers are not idempotent. |

#### Design notes

- **Button action immutability**: The `btn.Click` handler is wired in `Create` with
  a closure that resolves context values at click time via `context.Resolve()`.
  Update does not rewire it. If `component.Action` changes (unusual per spec), the
  stale handler fires the original event name. Document as "action wiring is
  immutable after Create."

- **Child control identity**: After `panel.Children.Clear()`, cleared controls are
  detached from the visual tree but remain in the renderer cache. The next
  `RenderChild`/`RenderChildren` call finds them in cache and calls their own
  `Update`. This is correct behavior.

---

### Unit 6 — Typed Components Phase A: JSON Polymorphism

**Risk:** Medium | **Files:** 2 modified, 2 new

Make `A2UiComponent` abstract and add `[JsonPolymorphic]` with `[JsonDerivedType]`
for all component types. **All properties stay on the abstract base.** Subtypes are
empty records that exist only for type discrimination.

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "component")]
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
[JsonDerivedType(typeof(TableComponent), "Table")]
[JsonDerivedType(typeof(SurfaceComponent), "Surface")]
public abstract record A2UiComponent { /* all properties remain here */ }
```

New `TypedComponents.cs`:

```csharp
public sealed record TextComponent : A2UiComponent;
public sealed record ButtonComponent : A2UiComponent;
// ... 18 more empty records
```

**Critical verification**: `[JsonPolymorphic]` with a `required` discriminator
property that is also a payload property must be verified early (first task in
Unit 6). If STJ conflicts, the `Component` property becomes `{ get; init; }`
(not `required`) on the base, and each subtype overrides it with a fixed
default value:

```csharp
public sealed record TextComponent : A2UiComponent
{
    public override string Component { get; init; } = "Text";
}
```

This ensures serialization round-trips correctly and constructing a
`new TextComponent { Id = "x" }` auto-populates the discriminator.

#### Tests

New `TypedComponentDeserializationTests.cs`: 20 round-trip tests confirming each
component type string deserializes to the correct C# subtype. All existing tests
pass unchanged because properties remain on the base.

---

### Units 7a–7d — Typed Components Phase B: Property Migration

**Risk:** High | **Files:** 10+ modified, 30+ test files updated

Split into four sub-units by component group to control blast radius. Each
sub-unit moves group-specific properties from the abstract base to the concrete
subtypes and updates all consumers.

#### Unit 7a — Layout Group

Components: `ColumnComponent`, `RowComponent`, `CardComponent`, `ListComponent`

Properties to migrate: `Justify`, `Align`, `Direction`, `Gap`

Catalog entries: `ColumnCatalogEntry`, `RowCatalogEntry`, `CardCatalogEntry`,
`ListCatalogEntry` — add downcast at entry point.

#### Unit 7b — Display Group

Components: `TextComponent`, `IconComponent`, `DividerComponent`,
`ImageComponent`, `VideoComponent`, `AudioPlayerComponent`

Properties to migrate: `Text`, `Label`, `Variant` (shared by Text/Button — may
need an intermediate `ITextContent` interface or keep on base), `Url`, `Name`,
`Fit`, `Axis`, `Alt`, `Description`

Design note: `Text`, `Label`, and `Variant` are used by both `TextComponent` and
`ButtonComponent`. Options:
- Keep them on the base (pragmatic; they are the most common properties)
- Introduce an abstract intermediate `TextualComponent` for components with text
- Duplicate the properties on each subtype

Decision: `Text` and `Label` (both `DynamicValue?`) stay on the abstract
`A2UiComponent` base — they are used by Text, Button, and most input components
(9 of 20 types). Moving them to an intermediate would force a complex
multi-inheritance hierarchy with no practical benefit. `Variant` is used only
by Text and Button; it stays on each subtype individually (duplicated, 2 sites).

#### Unit 7c — Input Group

Components: `TextFieldComponent`, `DateTimeInputComponent`,
`ChoicePickerComponent`, `CheckBoxComponent`, `SliderComponent`

Properties to migrate: `Value`, `Placeholder`, `Min`, `Max`, `Options`,
`DisplayStyle`, `Filterable`, `Rows`, `EnableDate`, `EnableTime`,
`ValidationRegexp`

Decision: `Checks` stays on `A2UiComponent` base — Button, all input
components, and potentially future components use it. `Value` moves to each
input subtype individually (5 sites) since it is not used by display or layout
components.

#### Unit 7d — Interactive Group

Components: `ButtonComponent`, `TabsComponent`, `ModalComponent`,
`TableComponent`, `SurfaceComponent`

Properties to migrate: `Action`, `Tabs` (tab definitions), `Trigger`, `Content`
(for Modal), `Columns` (for Table)

#### Test migration strategy

Every test constructing `new A2UiComponent { Component = "Text", ... }` changes
to `new TextComponent { ... }`. The `Component` property is set automatically
by the subtype (discriminator). This is a mechanical find-replace operation
within each sub-unit's component group.

Approximate impact: ~150-200 construction sites across ~30 test files.

---

### Unit 8 — XML Documentation

**Risk:** None | **Files:** ~25 modified

#### Configuration

Add to each library `.csproj` (not test projects):

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

Add to test project `.csproj` files:

```xml
<NoWarn>$(NoWarn);CS1591</NoWarn>
```

#### Coverage levels

| Project | Level | Scope |
|---------|-------|-------|
| `A2Ui.Core` | NuGet-grade | All public types, methods, properties, parameters |
| `AgUi.Protocol` | Types + methods | All public classes/records/enums + public methods |
| `A2Ui.Avalonia` | Types + methods | All public interfaces, classes + public methods |

#### Specific gaps to fill

- `Surface` class: missing class-level `<summary>`
- `DynamicValue` hierarchy (post-Unit 3): all 6 subtypes need docs
- Typed component records (post-Unit 7): all 20 subtypes need docs
- `OneOf<T1,T2>` and `OneOf<T1,T2,T3>`: generic type params need `<typeparam>`
- `ICatalogEntry`, `IRenderContext`: method params need `<param>` tags

---

## Property Placement After Phase B

Properties remaining on `A2UiComponent` base (used by many component types):

| Property | Reason to keep on base |
|----------|----------------------|
| `Id` | Universal |
| `Component` | Discriminator |
| `Parent` | Universal (adjacency list) |
| `Child` | Used by Card, Modal, and others |
| `Children` | Used by layout and container types |
| `Accessibility` | Universal |
| `Weight` | Used by any component in a weighted layout |
| `Checks` | Used by Button + all 5 input types |
| `Text` | Used by Text, Button, and input types (9 of 20) |
| `Label` | Used by Text, Button, and input types |
| `ExtensionData` | Forward-compat catch-all |

Properties migrating to subtypes:

| Property | Target subtype(s) |
|----------|------------------|
| `Variant` | TextComponent, ButtonComponent |
| `Action` | ButtonComponent |
| `Url` | ImageComponent, VideoComponent, AudioPlayerComponent |
| `Name` | IconComponent |
| `Fit` | ImageComponent |
| `Alt` | ImageComponent |
| `Axis` | DividerComponent |
| `Justify`, `Align`, `Gap` | ColumnComponent, RowComponent |
| `Direction` | ColumnComponent, RowComponent, ListComponent |
| `Tabs` (tab defs) | TabsComponent |
| `Trigger`, (modal content) | ModalComponent |
| `Columns` | TableComponent |
| `Value` | TextFieldComponent, DateTimeInputComponent, ChoicePickerComponent, CheckBoxComponent, SliderComponent |
| `Placeholder` | TextFieldComponent |
| `Rows` | TextFieldComponent |
| `Min`, `Max` | SliderComponent, DateTimeInputComponent |
| `Options` | ChoicePickerComponent |
| `DisplayStyle`, `Filterable` | ChoicePickerComponent |
| `EnableDate`, `EnableTime` | DateTimeInputComponent |
| `ValidationRegexp` | TextFieldComponent |
| `Description` | TextComponent (usage hint desc) |

---

## Component Type Inventory

The full set of component types from `specification/v0_9/json/basic_catalog.json`:

| Type string | C# Record | Category |
|-------------|-----------|----------|
| `Text` | `TextComponent` | Display |
| `Icon` | `IconComponent` | Display |
| `Divider` | `DividerComponent` | Display |
| `Image` | `ImageComponent` | Display |
| `Video` | `VideoComponent` | Display |
| `AudioPlayer` | `AudioPlayerComponent` | Display |
| `Column` | `ColumnComponent` | Layout |
| `Row` | `RowComponent` | Layout |
| `Card` | `CardComponent` | Layout |
| `List` | `ListComponent` | Layout |
| `Button` | `ButtonComponent` | Interactive |
| `Tabs` | `TabsComponent` | Interactive |
| `Modal` | `ModalComponent` | Interactive |
| `Table` | `TableComponent` | Interactive |
| `Surface` | `SurfaceComponent` | Interactive |
| `TextField` | `TextFieldComponent` | Input |
| `DateTimeInput` | `DateTimeInputComponent` | Input |
| `ChoicePicker` | `ChoicePickerComponent` | Input |
| `CheckBox` | `CheckBoxComponent` | Input |
| `Slider` | `SliderComponent` | Input |

---

## Risk Summary

| Unit | Risk | Primary concern |
|------|------|-----------------|
| 0 — FormatString | Minimal | One-line registry change |
| 1 — EventType test | Minimal | Reflection-only, no production code |
| 2 — OneOf types | Low | Pure addition, no existing consumers |
| 3 — DynamicValue hierarchy | Medium | Compat properties must be correct or 15 test files break |
| 4 — Remove compat | Low | Compiler identifies all consumers; mechanical fixes |
| 5 — ICatalogEntry.Update | Low-Medium | UI behavior changes; needs headless test verification |
| 6 — Typed components A | Medium | `[JsonPolymorphic]` + `required` discriminator needs verification |
| 7a-d — Typed components B | High | 150-200 construction sites across 30+ test files |
| 8 — XML docs | None | Large diff, zero behavioral change |
