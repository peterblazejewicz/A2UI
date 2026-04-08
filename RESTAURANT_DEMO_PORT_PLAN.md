# A2UI Samples Porting Assessment — .NET/Avalonia

## Context

We have successfully ported the **Gallery v0.9** sample (`samples/client/avalonia/gallery_v0_9/`) — a server-less, fixture-driven demo that exercises all 18 basic catalog components. The next step is porting a **connected** (agent-backed) sample to validate the full round-trip: user input → agent → A2UI messages → rendered surfaces → user action → agent.

The Restaurant Demo is the premier candidate because it exercises the most protocol features with moderate complexity. The Python agent code can eventually be replaced with a .NET agent using Microsoft Agent Framework 1.0.

---

## Decisions Made

- **Client app name:** `Shell` → `samples/client/avalonia/Shell/` (PascalCase per .NET convention, matches Lit client naming)
- **Phase 1 transport:** A2A over HTTP (matches existing Python agent)
- **Phase 2 LLM provider:** Ollama for local development (no API key needed)
- **Composer scaffold:** Will be removed separately — not used as a starting point

---

## Complete Sample Inventory

| Sample | Type | LLM? | Protocol | Complexity | Porting Value |
|--------|------|------|----------|------------|---------------|
| **restaurant_finder** | Agent | Yes | A2A | ~650 LoC Python | **HIGH** — full round-trip, multi-surface, actions, data binding |
| **contact_lookup** | Agent | Yes | A2A | ~500 LoC Python | **MEDIUM** — Icon, avatar Image, rich layout |
| **orchestrator** | Agent | Yes | A2A | ~280 LoC Python | LOW — meta-routing, requires 3 sub-agents running |
| **component_gallery** | Agent | No | A2A | ~400 LoC Python | LOW — already covered by our Gallery v0.9 |
| **rizzcharts** | Agent | Yes | A2A | ~500 LoC Python | LOW — custom catalog (Chart, GoogleMap), needs Google Maps API |
| **gemini_enterprise** | Agent | Yes | A2A | unknown | LOW — Gemini-specific |
| **lit/shell** | Client | — | A2A | ~800 LoC TS | **HIGH** — universal A2A client, the Avalonia equivalent is what we need |
| **lit/gallery_v0_9** | Client | — | local | ~600 LoC TS | DONE — already ported to Avalonia |
| **tools/composer** | Tool | Yes | CopilotKit | ~5000 LoC TS/React | LOW — design tool, too web-specific |

---

## Priority 1: Restaurant Demo Port

### What the Demo Does (User Experience)

1. User types "Top 5 Chinese restaurants in New York" into a text input
2. Agent calls `get_restaurants()` tool → searches local JSON data
3. Agent generates A2UI `createSurface` + `updateComponents` + `updateDataModel` messages
4. Client renders a restaurant list (Cards with images, ratings, cuisine tags, "Book Now" buttons)
5. User clicks "Book Now" → `action` event sent back to agent with restaurant context
6. Agent generates a booking form surface (TextField, DateTimeInput, Button)
7. User fills form and submits → `action` event with form field values
8. Agent generates a confirmation card surface

### A2UI Protocol Features Exercised

- `createSurface`, `updateComponents`, `updateDataModel` (all three server→client operations)
- `action` client→server message (two round-trips: book + submit)
- Template `List` with `componentId` + `path` data binding (most complex binding scenario)
- `DateTimeInput` with both `enableDate` and `enableTime` (booking form, line 69-70 of `examples/0.9/booking_form.json`)
- Multi-surface management (list → booking form → confirmation)
- Action context gathering from data model at dispatch time

### What Needs to Be Built

#### A. Avalonia Shell Client (`samples/client/avalonia/Shell/`)

New project — fresh, not based on the Composer scaffold.

| Component | Purpose | Reuse from Gallery? |
|-----------|---------|-------------------|
| `IA2AClient` / `A2AAgentClient` | HTTP client: POST to A2A endpoint, parse `DataPart` responses | NEW — no HTTP code exists in Avalonia samples |
| `ShellViewModel` | MVVM: prompt input, send command, surface events, action dispatch | Pattern from `GalleryViewModel` |
| `ShellWindow.axaml` | UI: text input, send button, status indicator, `A2UiSurface` host | NEW layout, reuses `A2UiSurface` control |
| DI container (`Program.cs`) | Register services | Pattern from Gallery's `Program.cs` |
| `AgentConfig` | Agent URL, app title, hero image | Simple record |

**Key design insight:** The Lit shell's `A2UIClient.send()` method (`samples/client/lit/shell/client.ts`) is only ~50 lines. It:
1. Creates `A2AClient` from agent card URL (`/.well-known/agent-card.json`)
2. Sets `X-A2A-Extensions: https://a2ui.org/a2a-extension/a2ui/v0.8` header
3. Sends `TextPart` (user query) or `DataPart` (action events with `application/json+a2ui`)
4. Extracts `DataPart` items from response `Task.status.message.parts`

The C# equivalent using `HttpClient` + `System.Text.Json` would be similarly compact (~80-100 LoC).

**Action round-trip flow (critical path):**
1. User clicks "Book Now" button in rendered surface
2. `ButtonCatalogEntry` fires `ctx.FireUserAction("book_restaurant", payload, componentId)`
3. `A2UiRenderer.UserActionFired` event propagates to `ShellWindow`
4. `ShellViewModel` serializes action as v0.8 `A2UIClientEventMessage` (see wire format section below)
5. `A2AAgentClient.SendActionAsync()` wraps it as `DataPart` with `application/json+a2ui` mime type
6. Posts to agent → receives new surface messages → feeds to `SurfaceManager.Process()`

#### B. Agent (Phase 1: Python as-is)

**No changes to the Python agent.** The Avalonia client speaks the same A2A protocol as the Lit shell:
```bash
cd samples/agent/adk/restaurant_finder && uv run .
# Agent listens on http://localhost:10002
```

---

## Pre-Implementation Fixes (Required Before Phase 1)

### Fix 1: Action Wire Format — v0.8 `userAction` Envelope

**Problem:** The Python restaurant agent (`agent_executor.py:73-85`) expects the v0.8 action format:
```json
{
  "userAction": {
    "actionName": "book_restaurant",
    "context": { "restaurantName": "...", ... }
  }
}
```

But our `ClientToServerMessage` in `A2Ui.Core` is v0.9:
```json
{
  "version": "v0.9",
  "action": { "name": "book_restaurant", "surfaceId": "...", "context": { ... } }
}
```

The Lit shell (`app.ts:492-500`) sends v0.8 format with `userAction.name` (not `actionName`). Notably, there's also a bug in the Python restaurant agent: it reads `actionName` (line 85) while the Lit shell sends `name`. The contact_lookup agent was already fixed (`agent_executor.py:88-89`: "Fix: Check both 'actionName' and 'name'"), but restaurant_finder was not.

**Decision:** For Phase 1, the Shell client sends the **v0.8 `userAction` format** to match what the Python agent expects. This is what the Lit shell does. We implement this as a simple serialization helper in the Shell project (not in `A2Ui.Core` — that stays v0.9-pure). The helper produces:
```json
{ "userAction": { "name": "...", "surfaceId": "...", "sourceComponentId": "...", "timestamp": "...", "context": { ... } } }
```

Using `name` (not `actionName`) to match the Lit shell convention. If the Python restaurant agent's `actionName` lookup fails, that's a pre-existing upstream bug — our Shell should behave identically to the Lit shell.

**Future:** When we build the .NET agent (Phase 2), it will accept v0.9 `ClientToServerMessage` natively, and the Shell can switch to `A2Ui.Core.Messages.ClientToServerMessage` serialization.

### Fix 2: A2UI Extension Version Negotiation

**Problem:** The Lit shell hardcodes `X-A2A-Extensions: https://a2ui.org/a2a-extension/a2ui/v0.8`. The Python agent advertises both v0.8 and v0.9 (via `_schema_managers` dict in `agent.py:75-78`). Our Shell should negotiate properly.

**Decision:** For Phase 1, follow the Lit shell's approach — **hardcode v0.9** in the `X-A2A-Extensions` header. The Python agent's `try_activate_a2ui_extension()` will match v0.9 if available and select it. This gives us v0.9 messages from the agent (matching our `A2Ui.Core` model) while sending v0.8 actions back (matching the agent's parser). This asymmetry (v0.9 inbound, v0.8 outbound) is pragmatic and matches exactly what would happen if the Lit shell were updated to request v0.9.

**Future:** When we build the .NET agent (Phase 2), both directions will use v0.9.

### Fix 3: DateTimeInput — Add Time Picker Support

**Problem:** Our `DateTimeInputCatalogEntry` (`InputCatalogEntries.cs:75-118`) renders a `CalendarDatePicker` only. It ignores the `enableDate` and `enableTime` properties from `A2UiComponent`. The restaurant booking form (`booking_form.json:66-70`) sets both `enableDate: true` and `enableTime: true`. Without time support, the booking form is incomplete.

**Decision:** Update `DateTimeInputCatalogEntry` before Phase 1 to respect `enableDate`/`enableTime`:
- `enableDate: true, enableTime: false` (default) → `CalendarDatePicker` (current behavior)
- `enableDate: true, enableTime: true` → `CalendarDatePicker` + `TimePicker` in a horizontal `StackPanel`
- `enableDate: false, enableTime: true` → `TimePicker` only
- Output format: ISO 8601 (`yyyy-MM-ddTHH:mm:ss`) when both are enabled

**Files to modify:**
- `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/InputCatalogEntries.cs` — `DateTimeInputCatalogEntry`

### Fix 4: SurfaceManager — Add `Clear()` Method

**Problem:** The Lit shell calls `processor.clearSurfaces()` before processing each new response (`app.ts:519`). This ensures old surfaces are removed when a new prompt is sent. Our `SurfaceManager` has no equivalent — it only removes surfaces via `deleteSurface` messages from the server.

**Decision:** Add a `Clear()` method to `SurfaceManager` that removes all tracked surfaces and fires `SurfaceDeleted` for each. This is needed for the Shell's "new prompt clears old surfaces" behavior.

**Files to modify:**
- `agent_sdks/dotnet/src/A2Ui.Core/SurfaceManager.cs` — add `Clear()` method
- `agent_sdks/dotnet/tests/A2Ui.Core.Tests/` — add test for `Clear()`

---

## Microsoft Agent Framework 1.0 Compatibility Assessment

### Key Findings (researched April 7, 2026)

- **Released:** April 3, 2026 — GA 1.0.0 (core), preview (A2A/AG-UI hosting)
- **A2A support:** YES — preview packages `Microsoft.Agents.AI.A2A` and `Microsoft.Agents.AI.Hosting.A2A.AspNetCore`
  - Server: `app.MapA2A(agent, "/a2a")` exposes agent as A2A endpoint with agent card
  - Client: A2A client support available
  - `DataPart` conversion resolved (Dec 2025) — `DataPart` for JSON data works
- **AG-UI support:** YES — preview package `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`
  - Emits standard AG-UI SSE events (RUN_STARTED, TEXT_MESSAGE_CONTENT, TOOL_CALL_*, etc.)
  - Compatible with our `AgUi.Protocol` C# SDK
- **A2UI support:** NONE — no awareness of the A2UI UI protocol
- **LLM providers:** Azure OpenAI, OpenAI, Anthropic, Gemini, **Ollama** (local)
- **Target frameworks:** .NET 8.0, 9.0, 10.0

### What MS Agent Framework Provides vs. What We Need

| Capability | MS Agent Framework | Our Libraries | Gap? |
|------------|-------------------|---------------|------|
| Agent definition + tools | `AIAgent` + `AIFunctionFactory` | — | No gap |
| A2A server hosting | `MapA2A()` | — | No gap |
| AG-UI SSE streaming | `MapAGUI()` | `AgUi.Protocol` (client-side) | No gap |
| LLM integration (Ollama) | Built-in multi-provider | — | No gap |
| A2UI schema injection into prompts | — | `A2Ui.Core` has message model | **Gap — need prompt builder** |
| A2UI JSON parsing from LLM output | — | — | **Gap — need `A2uiStreamParser` equivalent** |
| A2UI DataPart creation | — | `A2Ui.Core.Messages` | We provide this |
| A2UI extension negotiation | — | — | **Gap — need A2A extension helpers** |

### Architecture for .NET Agent (Phase 2)

```
Microsoft.Agents.AI                         — agent + tool definitions
Microsoft.Agents.AI.OpenAI (or Ollama)      — LLM provider (Ollama for local dev)
Microsoft.Agents.AI.Hosting.A2A.AspNetCore  — A2A endpoint hosting
A2Ui.Core                                   — A2UI message model, validation
NEW: A2Ui.Agent (or inline in sample)       — schema injection, response parsing, extension negotiation
ASP.NET Core                                — HTTP hosting, static files (restaurant images)
```

### Three Gaps to Fill for .NET Agent

1. **A2UI Prompt Builder (C#):** Equivalent of Python `A2uiSchemaManager.generate_system_prompt()` — injects component catalog JSON schema + examples into LLM system prompt. Input: catalog JSON files from `specification/v0_9/`. Output: system prompt string with embedded schema.

2. **A2UI Response Parser (C#):** Equivalent of Python `parse_response()` / `A2uiStreamParser` — extracts A2UI JSON blocks delimited by `<a2ui>...</a2ui>` tags from LLM text output.

3. **A2UI A2A Extension Helpers (C#):** Equivalent of Python `a2ui.a2a` module:
   - `GetA2UiAgentExtension()` — builds extension metadata for agent card
   - `TryActivateA2UiExtension()` — negotiates version from client headers
   - `CreateA2UiPart()` — wraps A2UI JSON as A2A `DataPart` with `application/json+a2ui`

### Risk Assessment

- **Core packages (1.0.0 GA):** Stable — safe to depend on
- **A2A hosting (preview):** API may change — acceptable risk for samples; pin to exact preview version at implementation time
- **Ollama integration:** Confirmed working, no API key needed
- **A2UI integration is custom regardless:** The bridge code is ours to maintain
- **Mitigation for preview packages:** Pin exact versions in `Directory.Packages.props`, document fallback to GA-only subset if preview breaks

---

## Recommended Porting Roadmap

### Pre-req: Renderer & Core Fixes
**Scope:** Fix DateTimeInput time support + add SurfaceManager.Clear()
**Effort:** Small — ~50-80 LoC + tests
**Files:** `InputCatalogEntries.cs`, `SurfaceManager.cs`, test files

### Phase 1: Avalonia A2A Shell Client
**Scope:** `samples/client/avalonia/Shell/` — Avalonia equivalent of `lit/shell`
**Agent:** Existing Python `restaurant_finder` on localhost:10002
**Transport:** A2A over HTTP
**Wire format:** Request v0.9 extension (inbound messages), send v0.8 `userAction` envelope (outbound actions)
**Effort:** Medium — ~500-700 LoC C# (ViewModel, A2AClient, Window, DI setup)
**Validates:** Full round-trip, A2A transport, action dispatch, multi-surface rendering

### Phase 2: .NET Restaurant Agent with MS Agent Framework + Ollama
**Scope:** Port `restaurant_finder` agent to C# in `samples/agent/dotnet/restaurant_finder/`
**Stack:** `Microsoft.Agents.AI` + Ollama + `A2Ui.Core` + ASP.NET Core
**Package versions:** Pin exact preview versions at implementation time; document GA fallback
**Effort:** Medium-High — agent logic (~400 LoC) + A2UI bridge (~300 LoC)
**Validates:** Pure .NET end-to-end, no Python dependency

### Phase 3: Contact Lookup (incremental)
**Scope:** Port `contact_lookup` agent + extend Shell with config switching
**Effort:** Low-Medium — reuses Phase 1/2 infrastructure
**Validates:** Multi-app, `Icon` component, `Image` avatar variant, `Row` justify/align

### Phase 4 (Future): Orchestrator
**Scope:** Multi-agent routing in .NET
**Effort:** High — requires all sub-agents + surface-ownership routing
**Validates:** Production multi-agent deployment

---

## Key Files

### Pre-req fixes:
- `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/InputCatalogEntries.cs` — DateTimeInput time picker
- `agent_sdks/dotnet/src/A2Ui.Core/SurfaceManager.cs` — add `Clear()` method
- `agent_sdks/dotnet/tests/A2Ui.Core.Tests/SurfaceManagerTests.cs` — test for `Clear()`
- `renderers/avalonia/tests/A2Ui.Avalonia.Tests/` — test for DateTimeInput variants

### Phase 1 — New project (`samples/client/avalonia/Shell/`):
- `A2Ui.Avalonia.Shell.csproj` — project file (refs A2Ui.Core, A2Ui.Avalonia)
- `Program.cs` — DI container + Avalonia app builder
- `App.axaml` + `App.axaml.cs` — Application shell
- `ViewModels/ShellViewModel.cs` — MVVM state, send command, surface event handlers
- `Views/ShellWindow.axaml` + `.axaml.cs` — UI layout with `A2UiSurface` host
- `Services/A2AAgentClient.cs` — HTTP client for A2A protocol
- `Models/AgentConfig.cs` — agent endpoint configuration

### Existing reusable infrastructure (no changes needed):
- `renderers/avalonia/src/A2Ui.Avalonia/Controls/A2UiSurface.cs` — host control
- `renderers/avalonia/src/A2Ui.Avalonia/A2UiRenderer.cs` — rendering engine
- `agent_sdks/dotnet/src/A2Ui.Core/Messages/ClientMessages.cs` — v0.9 action model (used in Phase 2)

### Reference files (read during implementation):
- `samples/client/lit/shell/client.ts` — A2A client logic to port (50 LoC)
- `samples/client/lit/shell/app.ts` — shell UI logic to port; note `clearSurfaces()` at line 519
- `samples/client/lit/shell/configs/restaurant.ts` — restaurant config
- `samples/client/avalonia/gallery_v0_9/ViewModels/GalleryViewModel.cs` — MVVM pattern reference
- `samples/client/avalonia/gallery_v0_9/Program.cs` — DI setup reference
- `samples/agent/adk/restaurant_finder/agent_executor.py` — A2A protocol flow; note `actionName` vs `name` discrepancy at line 85

### Verification:
1. **Pre-req:** Run `dotnet test A2Ui.slnx` — all tests pass including new DateTimeInput and SurfaceManager.Clear() tests
2. **Phase 1 startup:** Start Python agent: `cd samples/agent/adk/restaurant_finder && uv run .`
3. **Phase 1 startup:** Start Avalonia shell: `dotnet run --project samples/client/avalonia/Shell/`
4. **Functional test — query:** Type "Top 5 Chinese restaurants in New York" → verify restaurant list renders with images and "Book Now" buttons
5. **Functional test — action:** Click "Book Now" → verify booking form renders with both date AND time pickers
6. **Functional test — submit:** Fill party size, select date+time, submit → verify confirmation card renders
7. **Payload contract test:** Log outbound action JSON, verify it contains `{ "userAction": { "name": "book_restaurant", "context": { "restaurantName": "...", ... } } }` with resolved data model values
8. **Surface lifecycle test:** Send a second query → verify old surfaces are cleared before new ones render
