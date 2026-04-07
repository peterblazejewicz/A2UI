# .NET/Avalonia A2UI Implementation — Status & Architecture

**Protocol version:** v0.9  
**Branch:** `feature/dotnet-avalonia-renderer`  
**Runtime:** .NET 10 (`global.json` pins SDK 10.0.100)  
**Last updated:** 2026-04-07  
**Status:** Feature-complete for intended v0.9 baseline scope

---

## 1. Purpose

This document is the primary reference for the .NET/C# implementation of the
[A2UI](https://github.com/google/A2UI) (Agent-to-UI) and AG-UI protocols.
It serves as both a status tracker and an architecture overview — a single place
to understand what is built, what is deferred, and why.

The implementation provides a complete v0.9 rendering pipeline: from AG-UI agent
events through A2UI message processing to Avalonia desktop controls.

---

## 2. Solution Structure

**Solution file:** `A2Ui.slnx` (8 projects, 38 hand-authored source files)

| Project | Path | Purpose |
|---------|------|---------|
| `AgUi.Protocol` | `agent_sdks/dotnet/src/AgUi.Protocol/` | AG-UI 28-event type model, SSE parser, tool-call accumulator |
| `A2Ui.Core` | `agent_sdks/dotnet/src/A2Ui.Core/` | A2UI message model, envelope validation, surface state, data model, protocol DTOs |
| `A2Ui.Avalonia` | `renderers/avalonia/src/A2Ui.Avalonia/` | Avalonia renderer: 18 catalog entries, function registry, bridge |
| `AgUi.Protocol.Tests` | `agent_sdks/dotnet/tests/AgUi.Protocol.Tests/` | Event serialization, SSE parser tests |
| `A2Ui.Core.Tests` | `agent_sdks/dotnet/tests/A2Ui.Core.Tests/` | Message, validation, data model, surface manager tests |
| `A2Ui.Avalonia.Tests` | `renderers/avalonia/tests/A2Ui.Avalonia.Tests/` | Headless renderer, catalog entry, integration, bridge tests |
| `A2Ui.Avalonia.Gallery` | `samples/client/avalonia/gallery_v0_9/` | Offline spec-example replay harness |
| `A2Ui.Avalonia.Composer` | `samples/client/avalonia/composer/` | Scaffold for future agent-connected app |

**Shared build settings** (`Directory.Build.props`): `Nullable: enable`, `TreatWarningsAsErrors: true`, `LangVersion: latest`, Roslynator + NetAnalyzers enabled.

**Test suite:** 472 tests (102 + 58 + 312), 0 failures.

---

## 3. Protocol Coverage

### 3.1 AG-UI Events (28/28)

All 28 AG-UI event discriminators are modeled as `sealed record` types with
`[JsonDerivedType]` polymorphism on `BaseEvent`. No external dependencies
beyond `Microsoft.Extensions.Logging.Abstractions`.

| Category | Events |
|----------|--------|
| Lifecycle | `RUN_STARTED` `RUN_FINISHED` `RUN_ERROR` `STEP_STARTED` `STEP_FINISHED` |
| Text | `TEXT_MESSAGE_START` `TEXT_MESSAGE_CONTENT` `TEXT_MESSAGE_END` `TEXT_MESSAGE_CHUNK` |
| Tool | `TOOL_CALL_START` `TOOL_CALL_ARGS` `TOOL_CALL_END` `TOOL_CALL_RESULT` `TOOL_CALL_CHUNK` |
| State | `STATE_SNAPSHOT` `STATE_DELTA` `MESSAGES_SNAPSHOT` `ACTIVITY_SNAPSHOT` `ACTIVITY_DELTA` |
| Reasoning | `REASONING_START` `REASONING_MESSAGE_START` `REASONING_MESSAGE_CONTENT` `REASONING_MESSAGE_END` `REASONING_MESSAGE_CHUNK` `REASONING_END` `REASONING_ENCRYPTED_VALUE` |
| Extension | `RAW` `CUSTOM` |

Supporting infrastructure:
- `SseEventParser` — async SSE stream reader with reconnection logging
- `ToolCallArgsAccumulator` — reassembles multi-part `TOOL_CALL_ARGS` deltas

### 3.2 A2UI Messages

**Server → Client (4/4):** `createSurface`, `deleteSurface`, `updateComponents`, `updateDataModel`

**Client → Server (2/2):** `action`, `error`

Both directions enforce envelope validation via `Validate()`:
- `A2UiMessage`: requires `version = "v0.9"` + exactly one operation (`oneOf` from `server_to_client.json`)
- `ClientToServerMessage`: requires `version = "v0.9"` + exactly one of action/error

### 3.3 Transport Metadata DTOs

These flow through transport initialization/metadata (A2A, MCP), not as A2UI surface operations:

| Type | Schema | Fields |
|------|--------|--------|
| `ServerCapabilities` | `server_capabilities.json` | `supportedCatalogIds`, `acceptsInlineCatalogs` |
| `ClientCapabilities` | `client_capabilities.json` | `supportedCatalogIds`, `inlineCatalogs` (raw JSON) |
| `ClientDataModel` | `client_data_model.json` | `version`, `surfaces` (surfaceId → JSON data model) |

### 3.4 Data Model

`DataModel` provides per-surface JSON state with:
- RFC 6901 JSON Pointer resolution (with `~0`/`~1` unescaping)
- `DynamicValue` resolution — literals (string/number/bool/array), data bindings, function calls
- Set/delete at arbitrary paths with intermediate node auto-creation
- Array index access, auto-expansion, and length queries

---

## 4. Renderer

### 4.1 Catalog (18/18 v0.9 components)

| Category | Components |
|----------|------------|
| Display | `Text` `Image` `Icon` `Video`* `AudioPlayer`* `Divider` |
| Layout | `Row` `Column` `List` `Card` `Tabs` `Modal` |
| Interactive | `Button` `TextField` `CheckBox` `ChoicePicker` `DateTimeInput` `Slider` |

*Video/AudioPlayer are text placeholders — Avalonia has no native media controls.

**Extension point:** `CatalogRegistry` maps type strings → `ICatalogEntry` implementations.
Each entry provides `Create()` (initial render) and `Update()` (in-place refresh).

### 4.2 Component Capabilities

| Capability | Details |
|------------|---------|
| Button variants | `default`, `primary`, `danger`, `borderless` (CSS class-based) |
| ChoicePicker | `mutuallyExclusive` → ComboBox; `multipleSelection` → ListBox+CheckBox or WrapPanel+ToggleButton. Supports `filterable` (AutoCompleteBox) and `displayStyle` (checkbox/chips) |
| Input write-back | All inputs use `InputHelper.NotifyValueChanged()` — updates data model then fires action |
| In-place updates | TextField, CheckBox, Slider, DateTimeInput, ChoicePicker (ComboBox). All focus-guarded |
| Check validation | `CheckHelper.ApplyChecks()` evaluates conditions on all inputs |
| Divider axis | Horizontal (default) and vertical orientations |
| Template expansion | `List` with `children.template` iterates data model arrays with scoped paths |

### 4.3 Functions (26 built-in)

`FunctionRegistry.CreateDefault()` registers:

| Group | Functions |
|-------|-----------|
| Formatting | `capitalize` `formatNumber` `formatCurrency` `formatDate` `formatString` `pluralize` |
| Arithmetic | `add` `subtract` `multiply` `divide` |
| Comparison | `equals` `not_equals` `greater_than` `less_than` |
| Logical | `and` `or` `not` |
| String | `contains` `starts_with` `ends_with` |
| Validation | `required` `email` `regex` `length` `numeric` |
| Action | `openUrl` |

---

## 5. Bridge Architecture

`AgentEventBridge` connects AG-UI event streams to the A2UI rendering pipeline.

### Threading Model

```
Agent thread ──WriteEventAsync──► Channel<BaseEvent> (bounded, backpressure)
                                       │
Background task ◄──ProcessLoopAsync────┘
   │  Accumulates TOOL_CALL_ARGS deltas
   │  Deserializes JSONL → A2UiMessage
   │  Calls msg.Validate()
   ▼
UI thread ◄──PostSafe()── SurfaceManager.Process()
   │  Lock for state mutation
   │  Events fired outside lock
   ▼
A2UiRenderer + A2UiSurface ── renders Avalonia controls
```

### Safety guarantees

- **PostSafe()** wraps all `Dispatcher.UIThread.Post` lambdas in try-catch, preventing subscriber exceptions from crashing the Avalonia dispatcher
- **SurfaceManager** warns on dropped operations (unknown surfaceId, duplicate create)
- **Root component** absence triggers a warning log after `updateComponents`
- **Malformed payloads** are logged and skipped; processing continues

---

## 6. Design Decisions

These are conscious choices, not unaddressed gaps. Each was evaluated during
code review and accepted with rationale.

| Decision | Rationale |
|----------|-----------|
| `DynamicValue` union covers `DynamicBoolean`/`DynamicString`/etc. | The spec's `DynamicBoolean` is a schema constraint, not a distinct runtime type. One union type handles all shapes correctly. |
| No chunk-event expansion helper | No reference implementation (Python, Java, Lit, Angular) provides this. Chunk events deserialize correctly; expansion is consumer-side. |
| No `sendDataModel` packaging in Core | Core stores the flag; packaging depends on transport (A2A, MCP). Belongs in Composer/transport layer. |
| Permissive `GetRootComponents()` fallbacks | Supports incremental loading and backward compatibility. Root absence is surfaced via warning log, not rejection. |
| ChoicePicker multi-select returns `false` from `Update()` | ComboBox variant updates in-place. Multi-select/filterable variants re-create due to state sync complexity. Acceptable trade-off. |
| `CultureInfo.GetCultureInfo("en-US")` with fallback | FormatNumber/FormatCurrency target en-US formatting. Falls back to InvariantCulture in globalization-invariant mode (Docker, trimmed apps). |

---

## 7. Verification

### Build & Test

```bash
dotnet build A2Ui.slnx --configuration Release   # 0 warnings, 0 errors
dotnet test A2Ui.slnx --configuration Release     # 472 passed, 0 failed
```

### Requirement Traceability

| Requirement | Implementation | Test Evidence |
|-------------|---------------|---------------|
| AG-UI 28 event discriminators | `BaseEvent.cs` (28 `[JsonDerivedType]`) | `EventSerializationTests.cs` |
| A2UI server envelope validation | `A2UiMessage.Validate()` | `A2UiMessageTests.cs` (12 validation tests) |
| A2UI client envelope validation | `ClientToServerMessage.Validate()` | `A2UiMessageTests.cs` |
| 18/18 catalog components | `CatalogRegistry.CreateDefault()` | `CatalogRegistryTests.cs` + integration tests |
| RFC 6901 JSON Pointer escaping | `DataModel.SplitPath()` | `DataModelTests.cs` (4 escaping tests) |
| Bridge E2E tool-call → surface | `AgentEventBridge` | `AgentEventBridgeTests.cs` (9 tests incl. 3 E2E) |
| Protocol negotiation DTOs | `ProtocolContracts.cs` | `ProtocolContractsTests.cs` (7 round-trip tests) |

---

## 8. Review History

| Date | Review | Outcome |
|------|--------|---------|
| 2026-04-07 | Initial review | 11 findings: 9 implemented, 3 disagreed with rationale |
| 2026-04-07 | Re-review | 7 findings: 5 implemented, 2 accepted as deferred-by-design |
| 2026-04-07 | Final verification | All remediations verified; feature-complete for v0.9 scope |

---

## 9. Future Work

| Priority | Item | Notes |
|----------|------|-------|
| Next | **Composer app** | Agent-connected round-trip over A2A/MCP transport |
| Next | **Transport bindings** | Wire `ProtocolContracts` DTOs into A2A AgentCard / MCP initialization |
| Later | **v0.10 review** | Evaluate `specification/v0_10/` when draft stabilizes |
| Later | **Media controls** | LibVLCSharp or similar for Video/AudioPlayer |
