# .NET A2UI Telemetry & Debug-Checks Plan

## Context

Peter has a working lit/Python reference run captured in `sample-restaurant-find-log.txt` at the repo root (~2375 lines, happy path, zero errors). It documents what the Python restaurant agent (`samples/agent/adk/restaurant_finder/`) and the lit shell (`samples/client/lit/shell/`) emit when the user queries "Top 5 Chinese restaurants in New York", clicks **Book Now** on the first card, fills the booking form, and submits. The .NET/Avalonia port of this workflow is being built under `samples/client/avalonia/Shell/` (Phase 1 scaffold landed in commits `cd5e0577` / `24c205d9`).

The problem: when the .NET Shell runs against the same Python agent, there is no observable parity. The .NET pipeline is largely uninstrumented — `A2AAgentClient.cs` has only three `LogDebug`/`LogWarning` calls, `SurfaceManager` has 8 `LoggerMessage` declarations but the Shell's DI passes it `NullLoggerFactory` so those never fire, `CatalogRegistry` and `ToolCallArgsAccumulator` have zero log sites, and there is no `ActivitySource` anywhere in the .NET codebase. When something breaks during a future demo run (communication / network / model / agent), there is no structured log to read, no correlation between events, and no way to tell if a missing surface is a rendering bug or a transport failure or an agent retry.

This plan defines:
1. **A debug playbook** — for each of the four failure categories (communication, network, model, agent), what to look for in logs, which fields to check, which files are the likely culprits.
2. **A phased telemetry implementation** — concrete log sites, ActivitySource names, structured field catalog, DI wiring fixes, ordered so that Phase 1 unblocks Restaurant Shell debugging on its own and later phases add correlation, tracing, and optional OpenTelemetry.
3. **Verification metrics/pointers** — a catalog of invariants the .NET Shell's logs should satisfy for any successful run, drawn directly from what is observable in the Python reference log.

The north star: **make the .NET Shell log comparable to `sample-restaurant-find-log.txt` on the dimensions that matter (surface counts, component types, message sequence, user actions), then add improvements (correlation IDs, per-stage latency, structured fields) that the Python reference lacks.**

This plan does NOT touch:
- `samples/client/lit/**` (reference only — see the fork-as-reference stance in `WINDOWS_SETUP.md`)
- `samples/agent/adk/restaurant_finder/**` (Python reference only)
- `sample-restaurant-find-log.txt` (the reference artifact)

## Architectural reality check (important)

An early draft of this plan assumed `A2AAgentClient` was an SSE-streaming client with "first-byte latency" and "stream closed" events. **It is not.** The actual implementation in `samples/client/avalonia/Shell/Services/A2AAgentClient.cs` is:

- Plain HTTP POST via `HttpClient.PostAsJsonAsync(_config.ServerUrl, request, ...)`
- Full response read at once via `ReadFromJsonAsync<A2ATaskResponse>(...)`
- All A2UI messages extracted from `response.Result.Status.Message.Parts` in one pass
- No streaming, no SSE, no per-chunk events at the client layer

The SSE parser at `agent_sdks/dotnet/src/AgUi.Protocol/Transport/SseEventParser.cs` is a separate facility used by `AgentEventBridge.ProcessLoopAsync` for a different scenario (likely direct AG-UI event consumption without A2A wrapping). **Whether `AgentEventBridge` is on the Restaurant Shell's hot path is an open question** (see "Open questions" at the end); the Shell code I read calls `A2AAgentClient` → `SurfaceManager` directly with no visible bridge.

This changes Phase 1 for the Shell's A2A client significantly:
- No "first byte" timing concept at the HTTP layer
- No stream-lifecycle events
- The Activity for `A2A.SendMessage` is just the POST round-trip duration
- Log sites are request-start / request-complete / request-failed (three sites, not six)

All references below reflect this correction.

---

## 1. Debug playbook — the four failure categories

For each category: symptoms → fields/scopes to query → green-path vs red-path shapes → likely-culprit files.

### 1.1 Communication issues (A2UI protocol semantics, message structure)

Protocol-level problems: malformed A2UI JSON, missing required fields, component types the catalog doesn't know, surface-ID mismatches, data-model path errors, validation exceptions.

**Symptoms to look for in logs:**

| Symptom | Log signal |
|---|---|
| Agent emits a message the .NET SDK rejects | `A2UiMessage.ValidationFailed` (new Phase 1 log site) |
| Message references a surface that was never created | `SurfaceManager.SurfaceUnknown` (existing, LoggerMessage EventId 6) |
| BeginRendering references a component id that is not `"root"` | `SurfaceManager.NoRoot` (existing, EventId 7) |
| Component type arrives that is not in the 18-entry catalog | `CatalogRegistry.CatalogLookupMiss` (new Phase 1) + `A2UiRenderer.UnknownComponentTypeRendered` (new Phase 1) |
| Renderer falls back to `[Unknown component: ...]` TextBlock | Currently **silent** — fix is Phase 1 (highest single leverage) |
| Template binding for `List` fails to resolve | `A2UiRenderer.TemplateResolveFailed` adjacent to existing `Warning 3` |

**Fields/scopes to query:**

- Scope: `A2UiRequest{TaskId, ContextId, MessageId}` (from Phase 2 correlation)
- Structured fields: `ComponentType`, `SurfaceId`, `ComponentId`, `MessageType` (one of `createSurface` | `beginRendering` | `surfaceUpdate` | `dataModelUpdate` | `deleteSurface`)
- Counter to check: `ComponentsUpdated.ComponentCount` per surface should match the agent's declared count

**Green-path excerpt (Restaurant Shell, per surface):**

```
INF A2AAgentClient.SendMessageStarted HttpUrl="http://localhost:10002/" RequestBytes=412 MessageId="..."
INF A2AAgentClient.SendMessageCompleted HttpStatus=200 ResponseBytes=18234 MessageCount=4 DurationMs=2130
INF SurfaceManager.SurfaceCreated SurfaceId="default"
INF SurfaceManager.BeginRendering SurfaceId="default" PrimaryColor="#D32F2F" FontFamily="Roboto"
INF SurfaceManager.ComponentsUpdated SurfaceId="default" ComponentCount=24 RootComponentId="root-column"
INF SurfaceManager.DataModelUpdated SurfaceId="default" PathCount=1 TopLevelKeys="restaurants"
DBG A2UiRenderer.TemplateInstantiated ComponentId="restaurant-list" TemplateType="Card" InstanceCount=5
```

**Red-path flags:**

- `A2UiMessage.ValidationFailed` with `RawMessageJson` showing the agent's malformed payload
- `SurfaceManager.SurfaceUnknown` event (routing to non-existent surface)
- `CatalogRegistry.CatalogLookupMiss` (type agent sent is not registered in the .NET catalog)
- `A2UiRenderer.UnknownComponentTypeRendered` (silent fallback about to render the placeholder)
- `TemplateInstantiated.InstanceCount == 0` despite a populated DataModel

**Likely-culprit files (open these first for protocol issues):**
- `agent_sdks/dotnet/src/A2Ui.Core/Messages/A2UiMessage.cs` — validation logic (`Validate()` throws but doesn't log today)
- `agent_sdks/dotnet/src/A2Ui.Core/SurfaceManager.cs` — surface state machine
- `renderers/avalonia/src/A2Ui.Avalonia/Catalog/CatalogRegistry.cs` — component type lookup
- `renderers/avalonia/src/A2Ui.Avalonia/A2UiRenderer.cs` — line ~78 silent fallback (Phase 1 fix)
- `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/*.cs` — per-type binding implementations

### 1.2 Network issues (HTTP transport, connection failures, malformed responses)

Transport-level problems: failed POST to port 10002 (agent not running), DNS/TLS errors, timeouts, HTTP errors other than 200, truncated response bodies.

**Symptoms to look for in logs:**

| Symptom | Log signal |
|---|---|
| Python agent not running | `A2AAgentClient.SendMessageFailed` with `ExceptionType="HttpRequestException"` and inner `SocketException(ConnectionRefused)` |
| Agent returns 5xx | `SendMessageFailed` with `HttpStatus=5xx` |
| Response has no parts | Existing `LogWarning("No parts in agent response")` at `A2AAgentClient.cs:107` — upgrade to structured `ResponseEmpty` log with `CorrelationId` |
| Deserialize failure on a DataPart | Existing `LogWarning(ex, "Failed to deserialize A2UI message from DataPart")` at `A2AAgentClient.cs:129` — upgrade to structured `DataPartDeserializeFailed` with `MessageIndex` |
| Request hangs > 30s | `SendMessageStarted` without matching `SendMessageCompleted` / `SendMessageFailed` — needs Activity or scope-based detection |

**Fields/scopes to query:**

- Activity name: `A2A.SendMessage` (Phase 2, `ActivityKind.Client`)
- Tags: `http.method`, `http.url`, `http.status_code`, `http.response_content_length`
- Structured fields: `MessageCount`, `ResponseBytes`, `DurationMs`, `CorrelationId`
- Scope: `A2ARequest{MessageId, CorrelationId}` from request start to completion

**Green path (HTTP):**

```
DBG A2AAgentClient.SendActionStarted HttpUrl="http://localhost:10002/" MessageId="abc-123"
INF A2AAgentClient.SendMessageStarted HttpUrl="..." RequestBytes=412 MessageId="abc-123"
INF A2AAgentClient.SendMessageCompleted HttpStatus=200 ResponseBytes=18234 MessageCount=4 DurationMs=2130 MessageId="abc-123"
```

**Red path:**

```
ERR A2AAgentClient.SendMessageFailed HttpUrl="..." DurationMs=52 ExceptionType="HttpRequestException"
  System.Net.Http.HttpRequestException: Connection refused (localhost:10002)
```

**Likely-culprit files:**
- `samples/client/avalonia/Shell/Services/A2AAgentClient.cs` — top priority, currently only 3 `LogDebug`/`LogWarning` calls with no structured fields
- `samples/client/avalonia/Shell/Program.cs` — `AddHttpClient` registration, no timeout configured, no `DelegatingHandler` for request/response instrumentation
- (If a streaming path exists through `AgentEventBridge`): `agent_sdks/dotnet/src/AgUi.Protocol/Transport/SseEventParser.cs` and `renderers/avalonia/src/A2Ui.Avalonia/AgentEventBridge.cs`

### 1.3 Model issues (DataModel state, value bindings)

State-level problems: `dataModelUpdate` applied to wrong path, valueMap binding fails to resolve, `List` template renders zero instances despite non-empty data, binding expressions reference unknown paths.

**Symptoms to look for:**

| Symptom | Log signal |
|---|---|
| `DataModelUpdated` count mismatches agent's emitted count | per-surface `DataModelUpdated.PathCount` off by one or zero |
| `List` renders zero rows when reference expects 5 | `TemplateInstantiated{InstanceCount=0}` (new Phase 1 log) |
| Binding expression uses unknown function | `FunctionRegistry.UnknownFunction` (existing EventId 1) |
| Binding expression evaluates to error | `FunctionRegistry.EvalError` (existing EventId 2) |
| Check predicate fails | `CheckHelper.CheckEvalFailed` (existing EventId 1) |

**Fields/scopes to query:**

- Structured fields: `DataPath`, `ValueKind` (string/number/object/array), `ItemCount`, `BindingExpression`
- Scope: `Surface{SurfaceId}` to filter all model events for one surface
- Counter check: per-surface `DataModelUpdated.PathCount` cumulative; final state should match Python reference

**Green path (Restaurant Shell list data flow):**

```
INF SurfaceManager.BeginRendering SurfaceId="default"
INF SurfaceManager.DataModelUpdated SurfaceId="default" PathCount=1 TopLevelKeys="restaurants"
INF SurfaceManager.ComponentsUpdated SurfaceId="default" ComponentCount=24 RootComponentId="root-column"
DBG A2UiRenderer.TemplateInstantiated ComponentId="restaurant-list" TemplateType="Card" InstanceCount=5
```

**Red path:**

- `DataModelUpdated{TopLevelKeys="restaurants", ItemCount=0}` — agent sent an empty array
- `TemplateInstantiated{InstanceCount=0}` with a non-empty `DataModelUpdated` preceding it — binding path mismatch
- `FunctionRegistry.UnknownFunction` for `${restaurant.name}` or similar

**Likely-culprit files:**
- `agent_sdks/dotnet/src/A2Ui.Core/SurfaceManager.cs` — `ApplyDataModelUpdate` or equivalent
- `renderers/avalonia/src/A2Ui.Avalonia/Functions/FunctionRegistry.cs`
- The `List` catalog entry under `renderers/avalonia/src/A2Ui.Avalonia/Catalog/Entries/` (`LayoutCatalogEntries.cs`)
- `renderers/avalonia/src/A2Ui.Avalonia/A2UiRenderer.cs` — template instantiation path

### 1.4 Agent issues (LLM streaming, tool calls, agent-side validation)

Remote-side problems: Claude returns malformed A2UI JSON, agent retries internally, tools fail, agent gives up mid-flow, response contains an error TextPart.

From the .NET client's perspective these are mostly **invisible at the transport layer** — they show up as slow requests, missing surfaces, or error text parts in the response. The Python agent log has rich internal visibility (Attempt counters, validation banners, retry events) that the .NET client cannot see.

**Symptoms to look for in the .NET logs:**

| Symptom | Log signal |
|---|---|
| Request latency > 10s | `A2AAgentClient.SendMessageCompleted{DurationMs > 10000}` |
| Response contains only a TextPart with error text, no DataParts | `A2AAgentClient.ExtractA2UiMessages` returns empty list + new `ResponseHasErrorText` log |
| Expected surfaces missing at end of run | `SurfaceCount < 3` (Restaurant Shell full scenario expects `default`, `booking-form-*`, `confirmation`) — needs run-summary log |
| Expected components missing | `ComponentsUpdated.ComponentCount` well below reference baseline — verify against catalog |

**Fields/scopes to query:**

- Activity tag: `A2A.SendMessage.DurationMs`
- Structured fields: `ResponseHasErrorText`, `FinalSurfaceCount`, `FinalComponentCount` (run-summary log at shell shutdown)

**Green path (reference baselines from `sample-restaurant-find-log.txt`):**

- First LLM call (`get_restaurants`): ~2s (visible as `09:29:37` → `09:29:39` in LiteLLM stamps)
- Total response time per request: ~5-10s (estimated from log structure, no explicit timing)
- Three surfaces created over the full scenario

**Red path:**

- Request duration > 30s with no completion → Python agent retry loop hit or LLM hung
- `FinalSurfaceCount < 3` for the full Restaurant Shell scenario → agent gave up mid-flow
- Response TextPart containing "error" or "sorry" with no DataParts → LLM failed to generate valid A2UI after retries

**Likely-culprit files on the .NET side:**
- `samples/client/avalonia/Shell/Services/A2AAgentClient.cs` — must log enough that Peter can tell "look at .NET log" vs "pivot to Python agent log"
- `samples/client/avalonia/Shell/ViewModels/ShellViewModel.cs` — where response assembly decisions are made
- When the agent side is the suspect: read the Python agent log directly; the .NET log should just point there

---

## 2. .NET telemetry architecture

### 2.1 Stack decisions

| Concern | Choice | Rationale |
|---|---|---|
| Logging API | `Microsoft.Extensions.Logging.ILogger<T>` | Already in use; DI-friendly |
| Log generation | `[LoggerMessage]` source generators | Matches existing 18 declarations; zero-alloc; compile-time validated |
| Sink | Serilog (console + rolling file) | Already wired in Shell/Gallery `Program.cs`; `Enrich.FromLogContext()` already enabled |
| Tracing | `System.Diagnostics.ActivitySource` | BCL, zero deps, OTel-compatible when/if desired |
| Metrics | `System.Diagnostics.Metrics.Meter` (Phase 2+) | BCL, OTel-compatible |
| Distributed tracing export | **Optional** `OpenTelemetry.*` — Phase 3 only | Not a Phase 1/2 dependency; user choice |
| Test harness | Custom `TestLoggerProvider` + `TestActivityListener` | ~60 + ~30 lines; no new test deps |

### 2.2 Project layout decision

**Phase 1 & 2: inline declarations in existing projects.** Do NOT create a new `A2Ui.Diagnostics` shared project. The cost of an extra project exceeds the benefit at this scale. Match the established pattern: each project gets its `LoggerMessage` declarations adjacent to the code that uses them.

**Phase 3 reconsideration:** if and only if OpenTelemetry is adopted, create `agent_sdks/dotnet/src/A2Ui.Diagnostics/` hosting `ActivitySources`, `Meters`, `LogProperties` constant classes and a `TelemetryExtensions` DI wiring helper. That isolates the OTel dependency surface from `A2Ui.Core` and `AgUi.Protocol`, which should remain telemetry-source-only.

### 2.3 ActivitySource naming (Phase 2)

Five sources, one per logical layer. OTel vendor-module convention:

| Source name | Hosted in project | Example span names | ActivityKind |
|---|---|---|---|
| `A2Ui.AgUi.Protocol` | `agent_sdks/dotnet/src/AgUi.Protocol` | `SseStream.Read`, `ToolCallArgs.Accumulate` | Internal |
| `A2Ui.Core` | `agent_sdks/dotnet/src/A2Ui.Core` | `Surface.Create`, `Surface.ComponentsUpdate`, `Message.Validate` | Internal |
| `A2Ui.Avalonia.Renderer` | `renderers/avalonia/src/A2Ui.Avalonia` | `Renderer.Render`, `Renderer.ResolveTemplate` | Internal |
| `A2Ui.Avalonia.Bridge` | `renderers/avalonia/src/A2Ui.Avalonia` | `EventBridge.DispatchEvent`, `EventBridge.ProcessLoop` | Consumer |
| `A2Ui.Shell.A2AClient` | `samples/client/avalonia/Shell` | `A2A.SendMessage` | Client |

Each declared as `internal static readonly ActivitySource Source = new("A2Ui.X.Y", Assembly.Version);` in a `Diagnostics.cs` file in the project root, alongside existing `LoggerMessage` declarations.

### 2.4 Structured field catalog (use these exact names across all projects)

**Correlation (Phase 1 uses them as explicit params; Phase 2 adds them as scope properties):**
- `TaskId` — A2A task identifier
- `ContextId` — A2A context identifier
- `MessageId` — A2A message identifier (`A2AMessage.MessageId` at `A2AAgentClient.cs:84`)
- `CorrelationId` — Shell-generated GUID tying an outbound request to all downstream logs (new in Phase 1)

**Surface & component:**
- `SurfaceId`, `ComponentId`, `ComponentType`, `ParentComponentId`
- `ComponentCount`, `SurfaceCount`
- `RootComponentId`

**Message:**
- `MessageType` — one of `createSurface` | `beginRendering` | `surfaceUpdate` | `dataModelUpdate` | `deleteSurface`
- `MessageIndex` — sequence number within a single A2A response
- `MessageCount` — total in a response

**Data model:**
- `DataPath`, `ValueKind`, `ItemCount`, `TopLevelKeys` (joined string of top-level keys)

**Network:**
- `HttpUrl`, `HttpStatus`, `RequestBytes`, `ResponseBytes`, `DurationMs`

**User interaction (ClientEvent):**
- `ActionName`, `SourceComponentId`, `SurfaceId` (note the overlap — same key as the render field)

**AG-UI events (if/when `AgentEventBridge` is instrumented):**
- `EventType`, `EventIndex`

### 2.5 DI wiring fixes required in Phase 1

Three projects currently get `NullLoggerFactory` because the Shell's `ConfigureServices` does not pass a factory when constructing them:

| Component | Current state | Fix |
|---|---|---|
| `SurfaceManager` | `services.AddSingleton<SurfaceManager>();` at `Program.cs:61` resolves via reflection and the optional `ILoggerFactory?` param gets `null` → `NullLoggerFactory.Instance` | Change `SurfaceManager` ctor to take `ILogger<SurfaceManager>` directly (non-optional); DI will inject. Tests can pass `NullLogger.Instance`. |
| `CatalogRegistry` | No ILogger param today; zero log sites | Add optional `ILogger<CatalogRegistry>? logger = null` defaulting to `NullLogger<CatalogRegistry>.Instance`; register in Shell DI explicitly |
| `ToolCallArgsAccumulator` | No ILogger param today; zero log sites | Same pattern — optional parameter with NullLogger default |
| `A2AAgentClient` | Already gets `ILogger<A2AAgentClient>` via `AddHttpClient<>` | No wiring change; just add log sites inside methods |

**Phase 2 addition:** register a `LoggingHttpMessageHandler : DelegatingHandler` via `services.AddHttpClient<IA2AClient, A2AAgentClient>(...).AddHttpMessageHandler<LoggingHttpMessageHandler>()`. This gives dual visibility: the handler logs raw HTTP (URL, status, bytes, duration) and `A2AAgentClient` logs A2A-semantic events (extension negotiation, message count, surface count). Both are valuable.

### 2.6 Sensitive-data policy

Three categories of potentially sensitive data:
- LLM prompt text (user's query in a TextPart)
- LLM response text (TextMessageContent / TextPart in the response)
- A2UI DataModel values (form field contents, personal info)

Policy, matching the Python reference's `[:200]` truncation convention:

| Level | What to log |
|---|---|
| **Information and above** | Counts and shapes only (`RequestBytes`, `PartCount`, `ItemCount`) — no content |
| **Debug** | First 200 chars of text fields (matches `agent_executor.py` `part.text[:200]`) |
| **Trace** | Full payloads (raw JSON, full prompt, full response) — debugging forensics |
| **Never** | Auth headers, API keys, bearer tokens at any level |

Serilog in Shell/Gallery defaults to `Debug` level today — so by default we get truncated previews, matching reference behavior. If we add CI/automated verification, that environment should set `Information` to reduce log bulk.

### 2.7 LoggerMessage vs message templates

Use `[LoggerMessage]` source generators for every new log site. Matches existing pattern, compile-time validated, zero alloc, mechanically assertable by EventId in tests. The existing exceptions where templates leak into call sites (all `LogDebug`/`LogWarning` in `A2AAgentClient.cs:43, 52, 64, 107, 129, 133`) should be migrated to `LoggerMessage` declarations as part of Phase 1.

---

## 3. Phased implementation

### Phase 1: Minimum viable for Restaurant Shell debugging (~20 new log sites, 1 DI fix)

**Must-have before Peter can compare a .NET run against `sample-restaurant-find-log.txt`.** Delivers a text log that can sit side-by-side with the Python reference for visual diffing.

**File-by-file changes:**

| # | File | Change | Complexity |
|---|---|---|---|
| 1 | `samples/client/avalonia/Shell/Services/A2AAgentClient.cs` | Migrate 3 existing `Log*` calls to `[LoggerMessage]` declarations. Add `SendMessageStarted`, `SendMessageCompleted`, `SendMessageFailed` with full structured fields (`HttpUrl`, `RequestBytes`, `ResponseBytes`, `HttpStatus`, `MessageCount`, `DurationMs`, `CorrelationId`). Generate a per-request `CorrelationId` GUID at the top of `SendAsync`. Wrap each method with a `try`/`catch`/`Stopwatch` block so `SendMessageFailed` always fires on exception. | medium |
| 2 | `samples/client/avalonia/Shell/Program.cs` | Change `services.AddSingleton<SurfaceManager>()` to resolve with an explicit `ILogger<SurfaceManager>` from DI (preferred: update `SurfaceManager` ctor to take `ILogger<SurfaceManager>` directly). Explicitly register `CatalogRegistry` via DI so its logger is wired. | small |
| 3 | `agent_sdks/dotnet/src/A2Ui.Core/SurfaceManager.cs` | Extend existing `ComponentsUpdated` log (EventId 3) with `RootComponentId`, `RootComponentType`. Extend `DataModelUpdated` (EventId 4) with `PathCount`, `TopLevelKeys`. Add new `BeginRendering` log (new EventId 9) with `SurfaceId`, `PrimaryColor`, `FontFamily`. Add `MessageDispatched` (new EventId 10) at top of `Apply()` / dispatch entry with `MessageType`, `MessageIndex`, `SurfaceId`. | small |
| 4 | `agent_sdks/dotnet/src/A2Ui.Core/Messages/A2UiMessage.cs` | Add `ValidationFailed` log site that fires BEFORE `Validate()` throws. Fields: `MessageType`, `ValidationError`, first 200 chars of `RawMessageJson`. Requires optional `ILogger? logger = null` parameter added to `Validate(...)` method or a pre-throw hook in the caller (SurfaceManager). | tiny |
| 5 | `renderers/avalonia/src/A2Ui.Avalonia/Catalog/CatalogRegistry.cs` | Add optional `ILogger<CatalogRegistry>? logger = null` ctor param. Add `CatalogEntryRegistered` log (Debug) fired once per `Register` call, fields: `ComponentType`, `EntryTypeName`. Add `CatalogLookupMiss` log (Warning) fired when `TryGetEntry` returns false, fields: `ComponentType`, `KnownTypes` (joined). | small |
| 6 | `renderers/avalonia/src/A2Ui.Avalonia/A2UiRenderer.cs` | Add 3 new `LoggerMessage` declarations: `RenderComponent` (Debug) at top of `RenderComponent` with `ComponentId`, `ComponentType`, `ParentComponentId`; `UnknownComponentTypeRendered` (Warning) at the fallback path on **line ~78** — this is currently silent and is the highest-leverage single fix in Phase 1; `TemplateInstantiated` (Debug) whenever a `List` template materializes with `ComponentId`, `TemplateType`, `InstanceCount`. | small |
| 7 | `renderers/avalonia/src/A2Ui.Avalonia/AgentEventBridge.cs` | Add `EventDispatched` (Debug, `EventType`, `EventIndex`); `ProcessLoopStarted` (Information); `ProcessLoopCompleted` (Information, `EventCount`, `DurationMs`); `ProcessLoopFailed` (Error, exception); `A2UiMessageReceived` (Debug, per extracted A2UI message with `MessageType`, `MessageIndex`, `SurfaceId`). **Prioritize lower if verification shows the Shell hot path does not go through `AgentEventBridge`** — see open question. | small |
| 8 | `agent_sdks/dotnet/src/AgUi.Protocol/ToolCallArgsAccumulator.cs` | Add optional `ILogger? logger = null` param. Add `AccumulateStarted` (Debug, per new toolCallId), `AccumulateProgress` (Trace, per arg append), `AccumulateCompleted` (Debug, final length). Add orphan detection: if a toolCallId is never completed before disposal, log `AccumulateOrphaned` (Warning). | small |

**Phase 1 success criterion:** running the .NET Shell against the Python agent and performing the Restaurant Shell scenario produces a `logs/shell-<date>.log` that contains, in visible sequence: outbound `SendMessageStarted` → `SendMessageCompleted` → 3 × `SurfaceCreated` → `BeginRendering` → `ComponentsUpdated` with component counts → `DataModelUpdated` with restaurant item count → `TemplateInstantiated` → user click logged as new outbound `SendMessageStarted`. Zero warnings if the happy path holds.

### Phase 2: Correlation, ActivitySource, per-stage latency, test harness

After Phase 1 the .NET log is comparable to the Python log line-by-line. Phase 2 adds what the Python reference lacks: correlation across layers, per-stage timing, and assertable test helpers.

**File-by-file changes:**

| # | File | Change | Complexity |
|---|---|---|---|
| 9 | `agent_sdks/dotnet/src/AgUi.Protocol/Diagnostics.cs` (new) | `internal static readonly ActivitySource Source = new("A2Ui.AgUi.Protocol", ...)` | tiny |
| 10 | `agent_sdks/dotnet/src/A2Ui.Core/Diagnostics.cs` (new) | Same for `A2Ui.Core` | tiny |
| 11 | `renderers/avalonia/src/A2Ui.Avalonia/Diagnostics.cs` (new) | Two sources: `A2Ui.Avalonia.Renderer` and `A2Ui.Avalonia.Bridge` | tiny |
| 12 | `samples/client/avalonia/Shell/Diagnostics.cs` (new) | `A2Ui.Shell.A2AClient` source | tiny |
| 13 | `samples/client/avalonia/Shell/Services/A2AAgentClient.cs` | `StartActivity("A2A.SendMessage", ActivityKind.Client)` wrapping the POST. Tags: `http.method=POST`, `http.url`, `http.status_code`, `http.response_content_length`, `a2ui.message_count`, `a2a.correlation_id`. `RecordException` on failure. Add root `BeginScope({CorrelationId, MessageId})` at method entry. | small |
| 14 | `renderers/avalonia/src/A2Ui.Avalonia/AgentEventBridge.cs` | Wrap `ProcessLoopAsync` in an Activity (`EventBridge.ProcessLoop`, Consumer) and per-event `DispatchEvent` (Internal). Add per-event `BeginScope({EventType, EventIndex})`. | small |
| 15 | `agent_sdks/dotnet/src/A2Ui.Core/SurfaceManager.cs` | Wrap the per-message dispatch in an Activity named `Surface.<MessageType>` (Internal). Add per-message `BeginScope({SurfaceId, MessageType, MessageIndex})`. | small |
| 16 | `renderers/avalonia/src/A2Ui.Avalonia/A2UiRenderer.cs` | Wrap root `Render` in an Activity (`Renderer.Render`, Internal). Do **not** wrap per-component rendering — cardinality explosion. | tiny |
| 17 | `samples/client/avalonia/Shell/Services/LoggingHttpMessageHandler.cs` (new) | `DelegatingHandler` logging raw HTTP requests/responses with duration and byte counts. Fires at lower level than `A2AAgentClient` so we see both the raw POST and the semantic wrapper. | small |
| 18 | `samples/client/avalonia/Shell/Program.cs` | Register `LoggingHttpMessageHandler` via `AddHttpClient<IA2AClient, A2AAgentClient>().AddHttpMessageHandler<LoggingHttpMessageHandler>()`. Register `LoggingHttpMessageHandler` as `services.AddTransient<LoggingHttpMessageHandler>()`. | tiny |
| 19 | `samples/client/avalonia/Shell/Services/RequestSummaryLogger.cs` (new) | Hosted event sink that listens for `A2A.SendMessage` Activity completion and emits a single-line `RequestSummary` log with all key fields (`DurationMs`, `MessageCount`, `SurfaceCount`, `ComponentCount`, `CorrelationId`). Gives a scan-friendly one-liner per request at Information level. | small |
| 20 | `agent_sdks/dotnet/tests/A2Ui.TestHelpers/A2Ui.TestHelpers.csproj` (new) | New shared test helper library referenced by all three existing test projects. | tiny |
| 21 | `agent_sdks/dotnet/tests/A2Ui.TestHelpers/TestLoggerProvider.cs` (new) | `ILoggerProvider` that captures all log entries as a list of records (`(EventId, LogLevel, Message, Dictionary<string,object?> Properties)`). Assertions via LINQ on the captured list. | medium |
| 22 | `agent_sdks/dotnet/tests/A2Ui.TestHelpers/TestActivityListener.cs` (new) | `ActivityListener` that captures all started/stopped activities with their tags. Exposes a list for LINQ assertions. | small |
| 23 | `agent_sdks/dotnet/tests/A2Ui.Core.Tests/*.csproj` + `renderers/avalonia/tests/A2Ui.Avalonia.Tests/*.csproj` + `agent_sdks/dotnet/tests/AgUi.Protocol.Tests/*.csproj` | Add `<ProjectReference Include="...A2Ui.TestHelpers.csproj" />` to each. Add a handful of tests asserting on new log sites (SurfaceManager dispatch logs, A2UiRenderer unknown-type fallback log, A2AAgentClient correlation flow). | medium |
| 24 | `A2Ui.slnx` | Add `A2Ui.TestHelpers` project to solution | tiny |

**Phase 2 success criterion:** a single happy-path request produces, for every `SurfaceManager.ComponentsUpdated` log line, the same `CorrelationId` structured field as the initiating `A2AAgentClient.SendMessageStarted` log line, AND an `Activity.Current` exists at that point with matching tags. Tests in `A2Ui.Core.Tests` and `A2Ui.Avalonia.Tests` assert on `TestLoggerProvider` captured entries for at least 4 representative scenarios (happy path, validation failure, unknown component type, no-root surface).

### Phase 3 (optional): OpenTelemetry, JSON sink, metrics, automated verification

**Only if Peter wants to export traces/metrics to an observability backend (Jaeger, Honeycomb, Datadog) or wants machine-diffable logs.**

**File-by-file changes:**

| # | File | Change | Complexity |
|---|---|---|---|
| 25 | `agent_sdks/dotnet/src/A2Ui.Diagnostics/A2Ui.Diagnostics.csproj` (new) | New shared project for OTel wiring | tiny |
| 26 | `agent_sdks/dotnet/src/A2Ui.Diagnostics/ActivitySources.cs` (new) | Centralized source name constants | tiny |
| 27 | `agent_sdks/dotnet/src/A2Ui.Diagnostics/Meters.cs` (new) | Counters (`a2ui.surfaces.created`, `a2ui.components.rendered`, `a2ui.unknown_component_types`, `a2ui.validation.failures`) and Histograms (`a2ui.a2a.request.duration_ms`, `a2ui.a2a.response.bytes`) | small |
| 28 | `agent_sdks/dotnet/src/A2Ui.Diagnostics/TelemetryExtensions.cs` (new) | `AddA2UiTelemetry(IServiceCollection)`, `AddA2UiTracing(TracerProviderBuilder)`, `AddA2UiMetrics(MeterProviderBuilder)` | small |
| 29 | `Directory.Packages.props` | Add `OpenTelemetry`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Exporter.Console`, `OpenTelemetry.Exporter.OpenTelemetryProtocol` | tiny |
| 30 | `samples/client/avalonia/Shell/Program.cs` | Add `Serilog.Formatting.Compact.CompactJsonFormatter` JSON sink writing `logs/shell-<date>.jsonl` alongside the text log. Optional OTel wiring gated on environment variable `A2UI_OTEL_ENABLED=1`. | small |
| 31 | `tools/log-verify/log-verify.csproj` + `Program.cs` (new, optional) | CLI that reads `shell-<date>.jsonl` and asserts the verification checklist from section 4 below, exits 0 on pass, 1 on fail. Useful for local verification runs and future CI gating. | medium |

**Phase 3 success criterion:** the `.jsonl` log file is machine-parseable and contains all structured fields; `log-verify` can run against it and return a pass/fail verdict. OTel export to a local Jaeger instance shows the full `A2A.SendMessage` span tree with the correct parent-child relationships.

---

## 4. Metrics / checks / pointers catalog

These are the **definitions** of correctness invariants — the things Peter checks in a future debug run. Phase 1 makes them all queryable via log filtering; Phase 3 automates them in `log-verify`.

### 4.1 Universal invariants (must hold for ANY successful run)

| # | Invariant | How to check |
|---|---|---|
| 1 | Every rendered surface was created first | `count(SurfaceCreated) == count(distinct SurfaceId in BeginRendering)` |
| 2 | No messages routed to unknown surfaces | `count(SurfaceUnknown) == 0` |
| 3 | No catalog lookup misses | `count(CatalogLookupMiss) == 0` |
| 4 | No render fallbacks | `count(UnknownComponentTypeRendered) == 0` |
| 5 | No validation failures | `count(ValidationFailed) == 0` |
| 6 | Every `SendMessageStarted` has a matching `SendMessageCompleted` or `SendMessageFailed` | pair them by `CorrelationId` |
| 7 | Catalog entry count at startup matches the expected 18 | `count(CatalogEntryRegistered) == 18` |
| 8 | No Error-level logs | `count(level=Error) == 0` |
| 9 | No subscriber-threw warnings | `count(BridgeLog.SubscriberThrew) == 0` |
| 10 | No HttpRequestException | `count(ExceptionType="HttpRequestException") == 0` |

### 4.2 Restaurant Shell scenario invariants (drawn from `sample-restaurant-find-log.txt`)

| # | Invariant | Reference value | How to check |
|---|---|---|---|
| 11 | Three surfaces created over the full run | `default`, `booking-form-<suffix>`, `confirmation` | `count(SurfaceCreated) == 3` |
| 12 | Surface IDs include `default` and `confirmation` literally | both present | `SurfaceId in {default, confirmation}` both appear |
| 13 | One surface has a `booking-form-*` prefix | LLM-randomized suffix (reference: `booking-form-xian`) | regex match `^booking-form-.+$` |
| 14 | Restaurants in first list | 5 items, each with 6 fields (name, rating, detail, address, imageUrl, infoLink) | `DataModelUpdated{DataPath="restaurants", ItemCount} == 5` at least once |
| 15 | List template instantiates 5 restaurant cards | reference shows 5 visible cards | `TemplateInstantiated{InstanceCount} == 5` for the restaurant list parent |
| 16 | Component types observed in the run | superset of {Column, Row, List, Card, Button, Text, Image, TextField, DateTimeInput, Divider} | distinct `ComponentType` across all `RenderComponent` log lines |
| 17 | Exactly 2 outbound user interactions | `book_restaurant`, `submit_booking` | distinct `ActionName` in outbound send events |
| 18 | Action source components | `submit-button`, `book-now-button` (or similar IDs visible in reference) | `SourceComponentId` values |
| 19 | Images rendered | 5 (one per restaurant) | `count(distinct ImageUrl)` or equivalent image-render logs |
| 20 | No image load failures | none in reference | `count(MediaLog.ImageLoadFailed) == 0` |

### 4.3 Performance pointers (informational, not pass/fail)

| # | Pointer | Green-path target | Where to find it |
|---|---|---|---|
| 21 | First request total duration | < 10000ms (reference implies ~5-10s including 2s LiteLLM call) | `A2A.SendMessage` Activity duration |
| 22 | User-click → confirmation surface visible | < 15000ms | time between second outbound `SendMessageStarted` and final `SurfaceCreated{SurfaceId="confirmation"}` |
| 23 | Per-surface render cost | < 100ms | `Renderer.Render` Activity duration per root render |
| 24 | No request > 30000ms | always | hard ceiling — alert if any `DurationMs > 30000` |

### 4.4 Sanity counters per failure category (Phase 3 automation target)

| Category | Counter formula | Threshold |
|---|---|---|
| Communication | `count(ValidationFailed) + count(SurfaceUnknown) + count(UnknownComponentTypeRendered) + count(CatalogLookupMiss)` | must be `0` |
| Network | `count(SendMessageFailed) + count(MalformedLine) + count(ProcessLoopFailed)` | must be `0` |
| Model | `count(FunctionRegistry.EvalError) + count(CheckHelper.CheckEvalFailed) + count(DataModelUpdated{ItemCount=0})` for expected-non-empty paths | must be `0` |
| Agent | `count(ResponseHasErrorText=true) + count(FinalSurfaceCount < 3 for full scenario)` | must be `0` |

---

## 5. Verification strategy — running .NET Shell against Python agent

### 5.1 Run procedure

1. Ensure Python agent works: `cd samples/agent/adk/restaurant_finder && uv run .` → listens on `http://localhost:10002`. Optionally redirect stdout to capture a companion Python log.
2. Build & launch .NET Shell: `dotnet run --project samples/client/avalonia/Shell/`. Serilog writes `samples/client/avalonia/Shell/bin/<config>/<tfm>/logs/shell-<date>.log` (confirm path after Phase 1 run).
3. Reproduce the reference scenario:
   - Type **"Top 5 Chinese restaurants in New York"** (exact text from reference log line ~127) and submit
   - Click **Book Now** on the first restaurant card
   - Fill the booking form with: party size `2`, reservation time any, dietary `meat` (matches reference)
   - Click **Submit**
4. Stop both processes. Compare `shell-<date>.log` against `sample-restaurant-find-log.txt` on the dimensions below.

### 5.2 What should match 1:1

| Concept | Reference | .NET expected |
|---|---|---|
| Surface count over full run | 3 | 3 |
| Surface IDs | `default`, `booking-form-<suffix>`, `confirmation` | exact for `default` and `confirmation`; pattern match for `booking-form-*` |
| Restaurant list item count | 5 | 5 |
| Distinct component types seen | 10 (Column, Row, List, Card, Button, Text, Image, TextField, DateTimeInput, Divider) | 10 (same) |
| Tool calls the agent made | 1 (`get_restaurants`) | 1 (observable as an AG-UI ToolCallStart if Bridge path is used; otherwise agent-internal only) |
| User interactions | 2 (`book_restaurant`, `submit_booking`) | 2 outbound `SendActionAsync` calls with those `ActionName` values |
| HTTP 200 responses | all | all |

### 5.3 What should legitimately differ

| Concept | Why |
|---|---|
| Timestamps | Different wall-clock times |
| Durations | Network latency, LLM retry variance |
| TaskId / ContextId / MessageId | Newly generated per run |
| Booking-form surface suffix | LLM randomization (`booking-form-xian` in reference; different every run) |
| Token-by-token `a2ui.a2a.parts` logs | Python-internal only; .NET log does not observe LLM token stream |
| LiteLLM/Claude internal logs | Not in .NET log (agent-internal) |
| Renderer-specific events (Avalonia controls instantiated) | Reference is lit; .NET uses Avalonia — different renderer-level events |
| Log prefixes | Reference has `[REST]`/`[SHELL]`; .NET is a single process so no prefix |

### 5.4 Red-flag checklist (any of these is a failed verification)

Run against the Phase 1+2 logs:

- [ ] Any `A2UiRenderer.UnknownComponentTypeRendered` warning
- [ ] Any `CatalogRegistry.CatalogLookupMiss` warning
- [ ] Any `A2UiMessage.ValidationFailed`
- [ ] Any `SurfaceManager.SurfaceUnknown`
- [ ] Any `SendMessageFailed` (unless deliberately testing disconnect)
- [ ] `SendMessageStarted` with no matching completion within 60s
- [ ] `count(SurfaceCreated) < 3` at end of full scenario
- [ ] `DataModelUpdated{TopLevelKeys="restaurants", ItemCount=0}` in the initial response
- [ ] `TemplateInstantiated{InstanceCount=0}` for the restaurant list
- [ ] Any Error-level log line
- [ ] `A2A.SendMessage` Activity duration > 30000ms (Phase 2)
- [ ] Any `ProcessLoopFailed` (if Bridge path is used)
- [ ] Any `BridgeLog.SubscriberThrew` (existing, EventId 1)

### 5.5 Comparing logs mechanically

- Phase 1: manual diff using a text editor with `--grep` or similar — the logs are readable and comparable with eyeballs
- Phase 2: `TestLoggerProvider` + unit tests assert on individual log events in xUnit
- Phase 3: `shell-<date>.jsonl` + `tools/log-verify/` runs the checklist as an exit-code-gated CLI — suitable for future CI gating if a .NET CI workflow is added

---

## 6. Critical files (execution targets, ordered by leverage)

1. **`samples/client/avalonia/Shell/Services/A2AAgentClient.cs`** — currently a black box with only 3 sparse log calls. Highest single-file leverage for Phase 1. Touch-point for all communication, network, and agent-issue diagnostics.
2. **`samples/client/avalonia/Shell/Program.cs`** — DI wiring for `SurfaceManager` ILogger propagation, `LoggingHttpMessageHandler` registration (Phase 2), optional JSON sink and OTel wiring (Phase 3).
3. **`agent_sdks/dotnet/src/A2Ui.Core/SurfaceManager.cs`** — central state machine, 8 existing LoggerMessages but they don't currently fire because Shell passes NullLoggerFactory. Fix DI wiring + extend existing logs with richer structured fields.
4. **`renderers/avalonia/src/A2Ui.Avalonia/A2UiRenderer.cs`** — line ~78 has a silent `[Unknown component: ...]` fallback that renders a placeholder TextBlock without logging. Fixing this alone eliminates an entire class of invisible failures.
5. **`renderers/avalonia/src/A2Ui.Avalonia/Catalog/CatalogRegistry.cs`** — zero log sites today; startup-registration confirmation + lookup-miss logging are Phase 1 essentials.
6. **`renderers/avalonia/src/A2Ui.Avalonia/AgentEventBridge.cs`** — already has 4 LoggerMessages, needs 5 more for event-dispatch observability. Priority depends on whether the Shell's hot path actually goes through it (see open question #1).
7. **`agent_sdks/dotnet/src/A2Ui.Core/Messages/A2UiMessage.cs`** — `Validate()` throws without logging. Add a pre-throw log site so validation failures leave a trace even when the caller catches the exception.

---

## 7. Risks and trade-offs

| Risk | Mitigation |
|---|---|
| **Log volume at Debug level** — Phase 1 logs every component rendered, every message dispatched. Could be 1000-3000 lines per full Restaurant Shell session. | Information is the default in Serilog config today. Debug is opt-in when actively debugging. Phase 2's `RequestSummary` single-line-per-request gives scan-friendly visibility even at Information. |
| **Scope propagation across async** — ILogger scopes do NOT flow across async boundaries automatically. Re-establish scopes at every layer entry AND always pass correlation IDs as explicit structured fields. | Every LoggerMessage at inner layers takes `CorrelationId` / `TaskId` / `SurfaceId` as explicit fields rather than relying on inherited scope. |
| **Test log coupling** — asserting on log messages couples tests to wording. | Always assert on `EventId` integers and structured property values, never on formatted message strings. |
| **Activity cardinality** — per-component spans would explode (potentially hundreds per render). | Phase 2 plan deliberately spans only at `A2A.SendMessage`, `EventBridge.ProcessLoop`, `EventBridge.DispatchEvent`, `Surface.<Op>`, and root `Renderer.Render`. Not per-component. |
| **NullLoggerFactory regressions** — adding required `ILogger` params breaks existing call sites. | Use optional params with `NullLogger<T>.Instance` default. Preserves source compatibility. Only the Shell's DI explicitly wires the real factory. |
| **`AgentEventBridge` may not be on the Shell's hot path** (see open question #1) — instrumenting it could be wasted work if the Restaurant Shell goes `A2AAgentClient` → `ShellViewModel` → `SurfaceManager` directly. | Phase 1 prioritizes `A2AAgentClient` and `SurfaceManager` explicitly; the Bridge work is listed but can be descoped to Phase 2 or later if the hot path excludes it. |

---

## 8. Open questions (resolve before or during execution)

1. **Does the Restaurant Shell's hot path go through `AgentEventBridge`?** `A2AAgentClient` is a request-response POST client that returns a `List<A2UiMessage>` directly. The shape suggests `ShellViewModel` feeds those messages to `SurfaceManager` directly, not through `AgentEventBridge`. Action: first step of Phase 1 execution is to `Read` `ShellViewModel.cs` and confirm. If it bypasses `AgentEventBridge`, drop Bridge instrumentation from Phase 1 priority list (keep for Phase 2 or later).

2. **Is the booking-form surface suffix (`-xian` in reference) LLM-randomized, stable per restaurant, or deterministic?** Affects verification check #13. Assumption: LLM-randomized; verification uses regex `^booking-form-.+$` rather than exact match. Confirm by running .NET Shell once with a different first-restaurant selection and observing the generated suffix.

3. **Where should `A2Ui.TestHelpers` live in the solution?** Proposed: `agent_sdks/dotnet/tests/A2Ui.TestHelpers/` as an internal-friendly shared test library referenced by all three test projects (`A2Ui.Core.Tests`, `AgUi.Protocol.Tests`, `A2Ui.Avalonia.Tests`). Alternative: one copy per test project (simpler but duplicative).

4. **OpenTelemetry opt-in or skip?** Phase 3 is optional. Recommendation: defer until Peter has a concrete need (Jaeger/Honeycomb/Datadog target). No Phase 1/2 work depends on it.

5. **Should the `.jsonl` log be a CI artifact?** There is no .NET CI workflow today (confirmed: `.github/workflows/*` has 16 workflows for lit/python/java/angular/etc., zero for .NET). Phase 3 `log-verify` is useful as a local manual assertion tool even without CI gating. Future-proofing: if a .NET CI workflow is added, uploading the `.jsonl` as an artifact and running `log-verify` is a natural Phase 3.5.

6. **`A2AAgentClient` retry / resilience** — current code is single-attempt with no Polly pipeline. Phase 2 Activity model assumes one attempt per `SendMessageAsync` call. If retries are added later (e.g., Polly `AddStandardResilienceHandler`), the Activity needs an `a2a.retry_attempt` tag and per-attempt sub-spans. Tracking this as a known future extension point, not a Phase 1 requirement.

7. **Will `sample-restaurant-find-log.txt` be regenerated** (e.g., Python agent logging improves, or the reference scenario changes)? If so, verification checklist constants (3 surfaces, 5 restaurants, 18 catalog entries) should be defined as named constants in a single place (e.g., `tools/log-verify/Expectations.cs`) rather than scattered across the plan/tests. Low priority.

---

## 9. Execution order summary

**Phase 1 (unblocks Restaurant Shell debugging):**
1. Fix DI in `Program.cs` so `SurfaceManager` gets a real `ILogger` — single most important change
2. `A2AAgentClient` structured logging (3 new LoggerMessage decls + migrate 3 existing)
3. `A2UiRenderer` silent-fallback fix at line ~78
4. `CatalogRegistry` startup + miss logs
5. `SurfaceManager` extended structured fields + `BeginRendering` + `MessageDispatched`
6. `A2UiMessage.Validate` pre-throw log
7. `AgentEventBridge` event-dispatch logs (priority demoted if hot-path excludes it)
8. `ToolCallArgsAccumulator` accumulation logs + orphan detection

**Phase 2 (correlation, tracing, tests):**
9. 5 `Diagnostics.cs` files with ActivitySource declarations
10. `StartActivity` wiring at the 5 designated sites
11. `BeginScope` at layer entries
12. `LoggingHttpMessageHandler` + DI registration
13. `RequestSummaryLogger` hosted listener
14. `A2Ui.TestHelpers` shared project + `TestLoggerProvider` + `TestActivityListener`
15. Adopt test helpers in 3 existing test projects with representative assertions

**Phase 3 (optional):**
16. `A2Ui.Diagnostics` shared project with OTel wiring
17. Meter + instrument declarations
18. JSON sink (`CompactJsonFormatter`) in Shell `Program.cs`
19. `tools/log-verify/` CLI for checklist assertion
20. Optional OTel exporter wiring gated by environment variable

---

## 10. Verification after each phase

**After Phase 1:**
- Run `dotnet build A2Ui.slnx --configuration Release` — must succeed with zero warnings
- Run `dotnet test A2Ui.slnx --configuration Release --no-build` — must pass (existing tests, no new assertions yet)
- Run the Python agent + .NET Shell scenario
- Verify the shell log contains, in order: `SendMessageStarted` → `SurfaceCreated{SurfaceId=default}` → `BeginRendering` → `ComponentsUpdated{ComponentCount > 10}` → `DataModelUpdated{TopLevelKeys="restaurants"}` → `TemplateInstantiated{InstanceCount=5}`
- Verify zero warnings/errors in the happy path

**After Phase 2:**
- Run the .NET Shell under `dotnet trace collect` or with a minimal `ActivityListener` enabled — confirm `A2A.SendMessage` activity appears with Client kind and correct tags
- Verify that a `BeginScope({CorrelationId})` at `A2AAgentClient` entry results in `CorrelationId` appearing on downstream `SurfaceManager.ComponentsUpdated` log lines (Serilog `Enrich.FromLogContext` will propagate it)
- Run existing test projects: new tests asserting on `TestLoggerProvider` captured entries must pass
- Verify `RequestSummary` appears exactly once per outbound request with all fields populated

**After Phase 3:**
- Verify `logs/shell-<date>.jsonl` is valid JSON-per-line and includes all structured fields
- Run `tools/log-verify logs/shell-<date>.jsonl` — exit 0 on happy path
- If OTel wiring enabled: point `OTLP_ENDPOINT` at a local collector, observe full trace in Jaeger UI
