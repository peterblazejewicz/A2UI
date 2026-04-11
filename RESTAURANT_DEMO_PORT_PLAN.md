# Restaurant Demo Port — Status

_Fork-specific status tracker for the Avalonia Restaurant demo. Last
updated 2026-04-11._

The Restaurant demo validates the .NET/Avalonia A2UI implementation
end-to-end: a user query hits an agent, the agent responds with A2UI
messages, the Shell client renders them, and user actions round-trip
back. It is the first connected sample after the offline Gallery port.

**Branch:** `feature/restaurant-demo-shell`

---

## Status

| Item | Status | Notes |
|---|---|---|
| Pre-req fixes (DateTimeInput time picker, SurfaceManager.Clear, v0.8 userAction envelope, A2A extension header) | ✅ Done | Shipped in pre-Phase-1 commits on this branch |
| Phase 1 — Shell client scaffold | ✅ Done | `samples/client/avalonia/Shell/` — `A2AAgentClient`, `UserActionSerializer`, `ShellViewModel`, `ShellWindow`, `Program.cs` DI |
| Phase 1 — Telemetry (Waves 1-3) | ✅ Done | Structured logging across `SurfaceManager`, `CatalogRegistry`, `A2UiRenderer`, `A2AAgentClient`, `ShellViewModel`; Shell DI fix wires real `ILoggerFactory`. See `docs/DOTNET_TELEMETRY_REFERENCE.md`. |
| Phase 1/2 — Telemetry (Slices A-D) | ✅ Done | `ActivitySource` tracing on the Shell hot path (`A2A.SendMessage` → `Surface.<Op>` → `Renderer.Render`), Serilog `BeginScope` correlation, `LoggingHttpMessageHandler` + `RequestSummaryLogger`, `A2Ui.TestHelpers` with 4 scenario tests, AG-UI streaming path instrumentation (future-proofing). |
| Phase 1 — Manual end-to-end verification | ⏳ Pending | See `VERIFICATION_STEPS_PLAN.md` for the 8-step test matrix against the Python `restaurant_finder` agent on `localhost:10002`. |
| Phase 2 — .NET agent via MS Agent Framework + Ollama | ⬜ Not started | Gated on Phase 1 verification. Research notes in `docs/DOTNET_PHASE2_AGENT_RESEARCH.md`. |
| Phase 3 — Contact lookup | ⬜ Not started | |
| Phase 4 — Orchestrator | ⬜ Not started | |
| Optional — Phase 3 OpenTelemetry export | ⬜ Not started | Only ship if an OTel backend (Jaeger/Honeycomb/Datadog) becomes in scope. |

---

## Design decisions (load-bearing)

These are the non-obvious choices the Shell makes. Each affects wire
compatibility with the Python agent and should be preserved when the
.NET agent replaces it in Phase 2.

- **Phase 1 transport:** A2A over HTTP, single request/response round-trip.
  The Shell is synchronous: `A2AAgentClient.SendAsync` POSTs, awaits, and
  returns a fully-assembled `List<A2UiMessage>` to the ViewModel. No SSE,
  no `AgentEventBridge`, no `ToolCallArgsAccumulator` on the hot path.
- **Outbound wire format:** v0.8 `userAction` envelope. The Python
  `restaurant_finder` agent reads `DataPart.data.userAction` and expects
  the field name `name` (matching the lit shell convention, not the
  Python ADK's own `actionName`). Our `UserActionSerializer` emits the
  v0.8 shape to stay compatible; `A2Ui.Core.Messages.ClientToServerMessage`
  stays v0.9-pure. When Phase 2 ships a .NET agent, both directions can
  switch to v0.9 natively.
- **Inbound wire format:** Request v0.9 via `X-A2A-Extensions` header.
  The Python agent's `try_activate_a2ui_extension()` will select v0.9 if
  available. Asymmetry is intentional: v0.9 inbound, v0.8 outbound.
- **`SurfaceManager.Clear()`:** called from `ShellViewModel.SendAsync`
  AND `HandleUserActionAsync` before each round-trip, so old surfaces
  are cleared on every prompt and every action. Mirrors the lit shell's
  `processor.clearSurfaces()` pattern.

---

## What the demo exercises

| A2UI feature | Coverage |
|---|---|
| `createSurface`, `updateComponents`, `updateDataModel` | All three server→client operations |
| `action` client→server | Two round-trips (book + submit) |
| Template `List` with `componentId` + `path` data binding | Most complex binding scenario |
| `DateTimeInput` with `enableDate` and `enableTime` | Booking form — validates pre-req fix #3 |
| Multi-surface management | list → booking form → confirmation card |
| Action context gathering | Resolved data-model values propagated into the outbound action payload |

---

## Components and reusable infrastructure

**Shell project** (`samples/client/avalonia/Shell/`):
- `A2Ui.Avalonia.Shell.csproj` — references `A2Ui.Core`, `A2Ui.Avalonia`
- `Program.cs` — DI container + Avalonia app builder
- `App.axaml` + `App.axaml.cs` — application shell
- `ViewModels/ShellViewModel.cs` — MVVM state + dispatch loop
- `Views/ShellWindow.axaml` + `.axaml.cs` — UI layout hosting `A2UiSurface`
- `Services/A2AAgentClient.cs` — HTTP client for A2A with v0.9 extension header
- `Services/UserActionSerializer.cs` — emits v0.8 `userAction` envelope
- `Services/LoggingHttpMessageHandler.cs` + `RequestSummaryLogger.cs` — Slice B telemetry
- `Models/AgentConfig.cs` — agent endpoint configuration

**Reused infrastructure (no changes needed):**
- `renderers/avalonia/src/A2Ui.Avalonia/Controls/A2UiSurface.cs`
- `renderers/avalonia/src/A2Ui.Avalonia/A2UiRenderer.cs`
- `agent_sdks/dotnet/src/A2Ui.Core/SurfaceManager.cs`
- `agent_sdks/dotnet/src/A2Ui.Core/Messages/`

**Reference files read during implementation:**
- `samples/client/lit/shell/client.ts` — A2A client logic ported (~50 LoC)
- `samples/client/lit/shell/app.ts` — shell UI logic; `clearSurfaces()` at line 519
- `samples/client/lit/shell/configs/restaurant.ts` — restaurant config
- `samples/client/avalonia/gallery_v0_9/ViewModels/GalleryViewModel.cs` — MVVM pattern
- `samples/agent/adk/restaurant_finder/agent_executor.py` — A2A protocol flow; `actionName` vs `name` at line 85

---

## Running the Python agent

```bash
cd samples/agent/adk/restaurant_finder && uv run .
# Agent listens on http://localhost:10002
```

No changes to the Python agent are required — the Avalonia Shell speaks
the same A2A protocol as the lit shell.

---

## Next action

Manual end-to-end verification run. See `VERIFICATION_STEPS_PLAN.md`
for the 8-step test matrix. On success, Phase 2 (.NET agent) can start;
see `docs/DOTNET_PHASE2_AGENT_RESEARCH.md` for the research scoping
that Phase 2.
