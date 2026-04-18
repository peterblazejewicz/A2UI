# .NET A2UI Telemetry Reference

_Last verified 2026-04-11 against `feature/restaurant-demo-shell` head._

This is the shipped telemetry contract for the .NET/Avalonia A2UI
implementation. Log sites, span names, tag keys, and scope property
keys are **public surface** for operators, test assertions, and future
OTel exporters — do not rename without updating this file and
adjusting the test helpers.

The planning artifact that produced this contract is preserved in
`git log` (commits `da2c3114`, `58bd617c`, and Waves 1-3 + Slices A-D).
Use this file for "what exists now", not "why or how we built it".

---

## 1. ActivitySources (4)

Each `Diagnostics.cs` file exposes one or two `ActivitySource` instances,
named on the OTel vendor-module convention. Version is derived from the
containing assembly.

| Source name | File | Consumers |
|---|---|---|
| `A2Ui.Core` | `agent_sdks/dotnet/src/A2Ui.Core/Diagnostics.cs` | `SurfaceManager.Process` |
| `A2Ui.Avalonia.Renderer` | `renderers/avalonia/src/A2Ui.Avalonia/Diagnostics.cs` (`RendererSource`) | `A2UiRenderer.Render` |
| `A2Ui.Avalonia.Bridge` | `renderers/avalonia/src/A2Ui.Avalonia/Diagnostics.cs` (`BridgeSource`) | `AgentEventBridge.ProcessLoopAsync` |
| `A2Ui.Shell.A2AClient` | `samples/client/avalonia/Shell/Diagnostics.cs` | `A2AAgentClient.SendAsync` |
| `A2Ui.AgUi.Protocol` | `agent_sdks/dotnet/src/AgUi.Protocol/Diagnostics.cs` | `ToolCallArgsAccumulator.Complete` |

---

## 2. Span names

| Span name | Kind | Source | Tags |
|---|---|---|---|
| `A2A.SendMessage` | Client | `A2Ui.Shell.A2AClient` | `http.method`, `http.url`, `http.status_code`, `http.response_content_length`, `a2a.correlation_id`, `a2a.message_id`, `a2ui.message_count` |
| `Surface.CreateSurface` / `.UpdateComponents` / `.UpdateDataModel` / `.DeleteSurface` | Internal | `A2Ui.Core` | `a2ui.surface_id`, `a2ui.message_type` |
| `Renderer.Render` | Internal | `A2Ui.Avalonia.Renderer` | `a2ui.surface_id`, `a2ui.catalog_id` |
| `EventBridge.ProcessLoop` | Consumer | `A2Ui.Avalonia.Bridge` | — (loop-scope) |
| `EventBridge.DispatchEvent` | Internal | `A2Ui.Avalonia.Bridge` | `a2ui.event_type`, `a2ui.event_index` |
| `ToolCallArgs.Complete` | Internal | `A2Ui.AgUi.Protocol` | `a2ui.tool_call_id`, `a2ui.total_length` |

Validation failures, network failures, and ProcessLoop errors call
`Activity.SetStatus(ActivityStatusCode.Error, ex.Message)` +
`Activity.AddException(ex)` before the catch rethrows.
`OperationCanceledException` is treated as normal termination and does
NOT set the error status.

On the Shell hot path, Activity parent-child wiring is automatic because
`ShellViewModel`'s dispatch loop runs synchronously on the thread that
opened `A2A.SendMessage`, so `Activity.Current` propagates via
`AsyncLocal` into every downstream `Surface.<Op>` child span and the
enclosing `Renderer.Render` span.

---

## 3. LoggerMessage categories

All log sites use `[LoggerMessage]` source-generated declarations. Assert
on `EventId` and structured `Properties[...]` values, never on formatted
message strings (the `Message` template can change without breaking the
contract).

### 3.1 `SurfaceManagerLog` (11 sites) — `agent_sdks/dotnet/src/A2Ui.Core/SurfaceManager.cs`

| EventId | Level | Name | Key properties |
|---|---|---|---|
| 1 | Information | `SurfaceCreated` | `SurfaceId`, `CatalogId`, `PrimaryColor`, `AgentDisplayName` |
| 2 | Information | `SurfaceDeleted` | `SurfaceId` |
| 3 | Debug | `ComponentsUpdated` | `SurfaceId`, `Count`, `RootComponentId`, `RootComponentType` |
| 4 | Debug | `DataModelUpdated` | `SurfaceId`, `PathCount`, `TopLevelKeys` |
| 5 | Warning | `DuplicateCreate` | `SurfaceId` |
| 6 | Warning | `UnknownSurfaceOp` | `Operation`, `SurfaceId` |
| 7 | Warning | `RootComponentMissing` | `SurfaceId` |
| 8 | Information | `SurfacesCleared` | `Count` |
| 9 | Debug | `MessageDispatched` | `MessageType`, `SurfaceId` |
| 10 | Warning | `ValidationFailed` | `MessageType`, `ValidationError` |
| 11 | Warning | `DataModelApplyFailed` | `SurfaceId`, `Path`, `Reason` |

### 3.2 `CatalogRegistryLog` (2 sites) — `renderers/avalonia/src/A2Ui.Avalonia/Catalog/CatalogRegistry.cs`

| EventId | Level | Name | Key properties |
|---|---|---|---|
| 1 | Debug | `CatalogEntryRegistered` | `ComponentType`, `EntryTypeName` |
| 2 | Warning | `CatalogLookupMiss` | `ComponentType`, `KnownTypes` |

### 3.3 `RendererLog` (13 sites) — `renderers/avalonia/src/A2Ui.Avalonia/A2UiRenderer.cs`

EventIds 1-10 are pre-Phase-1 and cover `DetachFromParent`, `Resolve`,
function-call, and data-model-write failures. Phase 1 additions:

| EventId | Level | Name | Key properties |
|---|---|---|---|
| 11 | Debug | `RenderComponent` | `ComponentId`, `ComponentType`, `ParentComponentId` |
| 12 | Warning | `UnknownComponentTypeRendered` | `ComponentId`, `ComponentType` |
| 13 | Debug | `TemplateInstantiated` | `ComponentId`, `TemplateType`, `InstanceCount` |

### 3.4 `A2AAgentClientLog` (10 sites) — `samples/client/avalonia/Shell/Services/A2AAgentClient.cs`

| EventId | Level | Name | Key properties |
|---|---|---|---|
| 1 | Debug | `FetchingAgentCard` | `Url` |
| 2 | Debug | `SendingTextQuery` | `TextPreview` (200-char max) |
| 3 | Debug | `SendingAction` | — |
| 4 | Warning | `ResponseHasNoParts` | `CorrelationId` |
| 5 | Warning | `DataPartDeserializeFailed` | `MessageIndex`, `CorrelationId`, Exception |
| 6 | Debug | `ExtractedMessages` | `MessageCount`, `CorrelationId` |
| 7 | Information | `SendMessageStarted` | `HttpUrl`, `RequestBytes`, `MessageId`, `CorrelationId` |
| 8 | Information | `SendMessageCompleted` | `HttpStatus`, `ResponseBytes`, `MessageCount`, `DurationMs`, `CorrelationId` |
| 9 | Error | `SendMessageFailed` | `HttpUrl`, `DurationMs`, `ExceptionType`, `CorrelationId`, Exception |
| 10 | Error | `AgentReturnedError` | `ErrorMessage`, `CorrelationId` |

### 3.5 `ShellViewModelLog` (1 site) — `samples/client/avalonia/Shell/ViewModels/ShellViewModel.cs`

| EventId | Level | Name | Key properties |
|---|---|---|---|
| 1 | Debug | `ProcessingMessages` | `MessageCount`, `Trigger` (`query` / `action`) |

### 3.6 `LoggingHttpMessageHandlerLog` (3 sites) — `samples/client/avalonia/Shell/Services/LoggingHttpMessageHandler.cs`

| EventId | Level | Name | Key properties |
|---|---|---|---|
| 1 | Debug | `RequestStarted` | `Method`, `Url`, `RequestBytes` |
| 2 | Debug | `RequestCompleted` | `Method`, `Url`, `HttpStatus`, `ResponseBytes`, `DurationMs` |
| 3 | Warning | `RequestFailed` | `Method`, `Url`, `DurationMs`, `ExceptionType`, Exception |

### 3.7 `RequestSummaryLoggerLog` (1 site) — `samples/client/avalonia/Shell/Services/RequestSummaryLogger.cs`

| EventId | Level | Name | Key properties |
|---|---|---|---|
| 1 | Information | `RequestSummary` | `Status` (`ok`/`failed`), `HttpUrl`, `HttpStatus`, `MessageCount`, `DurationMs`, `CorrelationId` |

Fires from an `ActivityListener` subscribed to `A2Ui.Shell.A2AClient`,
projecting each completed `A2A.SendMessage` span into a single scan-friendly
Information-level one-liner.

### 3.8 `BridgeLog` (9 sites) — `renderers/avalonia/src/A2Ui.Avalonia/AgentEventBridge.cs`

| EventId | Level | Name | Key properties |
|---|---|---|---|
| 1 | Error | `UserActionSubscriberThrew` | Exception |
| 2 | Warning | `MalformedA2UiLine` | `LinePreview`, Exception |
| 3 | Warning | `InvalidA2UiMessage` | `LinePreview`, Exception |
| 4 | Error | `UiThreadActionFailed` | Exception |
| 5 | Information | `ProcessLoopStarted` | `ChannelCapacity` |
| 6 | Information | `ProcessLoopCompleted` | `EventCount`, `DurationMs` |
| 7 | Error | `ProcessLoopFailed` | `EventCount`, `DurationMs`, Exception |
| 8 | Debug | `EventDispatched` | `EventType`, `EventIndex` |
| 9 | Debug | `A2UiMessageReceived` | `ToolCallId`, `MessageCount` |

### 3.9 `ToolCallArgsAccumulatorLog` (4 sites) — `agent_sdks/dotnet/src/AgUi.Protocol/ToolCallArgsAccumulator.cs`

| EventId | Level | Name | Key properties |
|---|---|---|---|
| 1 | Debug | `AccumulateStarted` | `ToolCallId` |
| 2 | Trace | `AccumulateProgress` | `ToolCallId`, `DeltaLength`, `TotalLength` |
| 3 | Debug | `AccumulateCompleted` | `ToolCallId`, `TotalLength` |
| 4 | Warning | `AccumulateOrphaned` | `ToolCallId`, `TotalLength` |

The Bridge + Accumulator categories are **off the Restaurant Shell hot
path** — instrumented for the AG-UI streaming code path when/if a .NET
streaming client is added later.

---

## 4. BeginScope properties (Serilog LogContext)

| Opened in | Key | Value source |
|---|---|---|
| `A2AAgentClient.SendAsync` (entry) | `CorrelationId` | `Guid.NewGuid().ToString("N")` — per-request |
| `A2AAgentClient.SendAsync` (entry) | `MessageId` | `Guid.NewGuid().ToString()` — matches `A2AMessage.MessageId` on the wire |
| `SurfaceManager.Process` (per message) | `SurfaceId` | Pattern-matched from the message's operation field |
| `SurfaceManager.Process` (per message) | `MessageType` | `A2UiOperationType.ToString()` or `"(empty)"` |

`Enrich.FromLogContext()` in `Program.cs` emits these as properties on every
log line inside the scope, so downstream sites inherit them without explicit
parameters. Scopes are `AsyncLocal`-backed and flow through the synchronous
`foreach` loop in `ShellViewModel` that dispatches each message into
`SurfaceManager.Process`.

---

## 5. Test harness — `A2Ui.TestHelpers`

New library at `agent_sdks/dotnet/tests/A2Ui.TestHelpers/` referenced by all
three existing test projects. Framework-agnostic — only depends on
`Microsoft.Extensions.Logging.Abstractions`.

### `TestLoggerProvider : ILoggerProvider`

```csharp
using var provider = new TestLoggerProvider();
using var factory = provider.CreateFactory();
var sm = new SurfaceManager(factory);
sm.Process(message);

// LINQ-assert on captured entries — never on formatted strings
var dispatched = provider.Entries
    .Where(e => e.EventId.Id == 9 && e.CategoryName == "A2Ui.Core.SurfaceManager")
    .ToList();
dispatched.Should().HaveCount(1);
dispatched[0].Properties["SurfaceId"].Should().Be("s-happy");
```

`TestLogEntry` is a `sealed record` with `CategoryName`, `Level`, `EventId`,
`Message`, `IReadOnlyDictionary<string, object?> Properties`, `Exception?`.

### `TestActivityListener : IDisposable`

```csharp
using var listener = new TestActivityListener("A2Ui.Core", "A2Ui.Avalonia.Renderer");
sut.DoWork();

var spans = listener.StoppedActivities
    .Where(a => (string?)a.GetTagItem("a2ui.surface_id") == "s-happy")
    .ToList();
spans.Should().Contain(a => a.OperationName == "Surface.CreateSurface");
```

The listener subscribes globally via `ActivitySource.AddActivityListener`.
**Parallel-test contamination gotcha:** two test classes creating listeners
at the same time will see each other's spans. Mitigate by filtering span
queries on a unique tag (the 4 shipped scenarios use unique per-test
`a2ui.surface_id` values: `s-happy`, `s-bad`, `s-noroot`, `s-unknown`) or
by using `[Collection("NonParallel")]` if filtering isn't feasible.

### Shipped scenario tests (4)

| Scenario | File | Asserts |
|---|---|---|
| Happy path | `A2Ui.Core.Tests/A2Ui/TelemetryScenarioTests.cs` | EventIds 9/1/3/4 fire with correct Properties; Activities `Surface.{CreateSurface,UpdateComponents,UpdateDataModel}` fire; EventIds 10 and 6 do NOT fire |
| Validation failure | same | `A2UiMessageValidationException` thrown; EventId 9 fires before EventId 10; Activity has `Status=Error` + exception recorded |
| No-root surface | same | EventId 7 fires; EventId 3 still fires with `RootComponentId="(none)"` |
| Unknown component type | `A2Ui.Avalonia.Tests/TelemetryScenarioTests.cs` | `CatalogRegistryLog` EventId 2 + `RendererLog` EventId 12 fire; returned control is `TextBlock` with `[Unknown component: …]`; `Renderer.Render` span fires |

---

## 6. Sensitive-data policy

Three categories of potentially sensitive data:

- LLM prompt / response text
- A2UI DataModel values (form fields, personal info)
- HTTP request / response bodies

| Level | What ships |
|---|---|
| `Information` and above | Counts and shapes only — no content |
| `Debug` | First 200 chars of text fields (matches Python reference convention) |
| `Trace` | Full payloads — forensic use only |
| never | Auth headers, API keys, bearer tokens at any level |

Serilog defaults to `Debug` in `Program.cs`. For automated / CI runs raise
to `Information` to reduce log bulk and avoid logging payload previews.

---

## 7. Out of scope / future work

- **Phase 3 (optional):** OpenTelemetry export via
  `OpenTelemetry.Exporter.OpenTelemetryProtocol`, a JSONL sink via
  `Serilog.Formatting.Compact.CompactJsonFormatter`, and a `log-verify`
  CLI that asserts invariants against a captured log file. Defer until
  there is a concrete OTel backend (Jaeger/Honeycomb/Datadog) or a CI
  workflow to gate on.
- **Shell-side unit tests:** there are no Shell tests today; the 4
  shipped scenario tests exercise `A2Ui.Core` and `A2Ui.Avalonia` only.
  The Shell is covered end-to-end via the manual Restaurant Shell
  verification run against the Python `restaurant_finder` agent.
- **Retry / resilience on `A2AAgentClient`:** single-attempt today. If a
  Polly pipeline is added, the `A2A.SendMessage` Activity needs an
  `a2a.retry_attempt` tag and per-attempt sub-spans.
