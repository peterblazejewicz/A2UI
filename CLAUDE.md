# A2UI .NET/Avalonia Implementation — Claude Code Workspace

## Project Goal

Build the first .NET/C# implementation of the A2UI (Agent-to-UI) protocol,
contributed directly to this fork of `google/A2UI` (Apache 2.0).

Deliverables living inside this repository:
- `agent_sdks/dotnet/` — C# SDK: AG-UI event types + A2UI message model
- `renderers/avalonia/` — Avalonia renderer: catalog registry + control implementations
- `samples/client/avalonia/` — Gallery v0.9 port (Shell client planned next)

**Fork:** `https://github.com/peterblazejewicz/A2UI`
**Branch:** `feature/dotnet-avalonia-renderer`
**Host:** Windows 11 Pro (development workstation)

> **Implementation status:** See [`docs/DOTNET_AVALONIA_IMPLEMENTATION.md`](docs/DOTNET_AVALONIA_IMPLEMENTATION.md)
> for the authoritative status tracker — protocol coverage, actor/component map,
> architecture diagrams, design decisions, and review history.

---

## Repository Structure (actual)

```
A2UI/                                  ← repo root (fork of google/A2UI)
├── CLAUDE.md                          ← you are here
├── A2Ui.slnx                         ← solution file (all 7 .NET projects)
├── global.json                        ← .NET SDK version pin
├── Directory.Build.props              ← shared MSBuild settings (Nullable, analyzers)
├── Directory.Packages.props           ← central package management
│
├── specification/                     ← A2UI protocol specs (read-only reference)
│   ├── v0_8/                          ← stable
│   │   ├── docs/
│   │   └── json/                      ← server_to_client.json, basic_catalog.json
│   ├── v0_9/                          ← current working version (use this)
│   │   ├── docs/a2ui_protocol.md      ← authoritative spec doc
│   │   └── json/
│   │       ├── server_to_client.json
│   │       ├── client_to_server.json
│   │       ├── basic_catalog.json
│   │       └── catalogs/              ← basic/ and minimal/ catalog examples
│   └── v0_10/                         ← proposed next version (draft, do not implement yet)
│
├── agent_sdks/                        ← SDK implementations per language
│   ├── python/                        ← existing reference implementation
│   ├── java/                          ← existing reference implementation
│   └── dotnet/                        ← OUR .NET/C# CODE
│       ├── src/
│       │   ├── AgUi.Protocol/         ← AG-UI 28-event types, SSE parser, tool-call accumulator
│       │   └── A2Ui.Core/             ← A2UI messages, validation, SurfaceManager, DataModel, ProtocolContracts
│       ├── tests/
│       │   ├── AgUi.Protocol.Tests/
│       │   └── A2Ui.Core.Tests/
│       └── .editorconfig
│
├── renderers/                         ← renderer libraries per platform
│   ├── lit/                           ← existing web renderer
│   ├── angular/                       ← existing Angular renderer
│   ├── web_core/                      ← shared web core
│   ├── markdown/
│   └── avalonia/                      ← OUR AVALONIA RENDERER
│       ├── src/
│       │   └── A2Ui.Avalonia/        ← catalog registry, 18 entries, function registry, bridge
│       └── tests/
│           └── A2Ui.Avalonia.Tests/  ← Avalonia.Headless.XUnit + integration tests
│
├── samples/
│   ├── agent/adk/                     ← Python reference agents
│   └── client/
│       ├── lit/                       ← existing web clients
│       ├── angular/                   ← existing Angular clients
│       └── avalonia/                  ← OUR NEW CODE
│           └── gallery_v0_9/          ← v0.9 local gallery (MVVM desktop app)
│
└── tools/
    └── composer/                      ← original web Composer (upstream, not ported)
```

---

## Spec Reference Paths

Always read the spec from the repo — do not rely on memory:

```bash
# Authoritative protocol spec
cat specification/v0_9/docs/a2ui_protocol.md

# Component catalog (defines all valid component type names)
cat specification/v0_9/json/basic_catalog.json

# Wire format schemas
cat specification/v0_9/json/server_to_client.json
cat specification/v0_9/json/client_to_server.json

# Catalog examples (used by Gallery app)
ls specification/v0_9/json/catalogs/basic/examples/
ls specification/v0_9/json/catalogs/minimal/examples/
```

Target **v0.9** for implementation. Review `specification/v0_10/` for forward-compatibility
hints but do not implement v0.10 features yet.

---

## Environment

.NET 10 — `dotnet` is on PATH:

```bash
dotnet --version    # 10.0.x
```

Optional environment variables:

```bash
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
```

---

## Build & Test Commands

Run from the repo root:

```bash
# Build everything (all 7 projects)
dotnet build A2Ui.slnx --configuration Release

# Test everything
dotnet test A2Ui.slnx --configuration Release --no-build

# Build individual projects if needed
dotnet build renderers/avalonia/src/A2Ui.Avalonia/A2Ui.Avalonia.csproj --configuration Release
dotnet build samples/client/avalonia/gallery_v0_9/A2Ui.Avalonia.Gallery.csproj --configuration Release
```

---

## Coding Standards

### Non-negotiable rules

- `Nullable` **enabled** everywhere — zero `!` suppressions without a comment
- `TreatWarningsAsErrors` **true** — fix warnings, never suppress
- **File-scoped namespaces** — `namespace Foo.Bar;` not `namespace Foo.Bar { }`
- `var` only when the type is obvious from the right-hand side
- No `async void` except event handlers — always try/catch inside
- `ConfigureAwait(false)` on all library awaits; `ConfigureAwait(true)` in ViewModels
- Records for immutable data (`sealed record`), classes for mutable/service types
- `CancellationToken` threaded through every async method signature

### Naming

| Thing | Convention |
|-------|-----------|
| Private fields | `_camelCase` |
| Constants | `PascalCase` |
| Interfaces | `IFoo` |
| Async methods | `FooAsync` |
| Test methods | `MethodName_StateUnderTest_ExpectedBehavior` |

### Formatting

```bash
dotnet csharpier .
```

A PostToolUse hook in `.claude/settings.json` auto-formats `.cs` files after
every Write/Edit — manual formatting is rarely needed.

---

## Git Workflow

```bash
git checkout feature/dotnet-avalonia-renderer

# Commit format — factual, no marketing language (matches repo convention)
git commit -m "feat(dotnet-sdk): implement AG-UI 28-event C# model

- Add BaseEvent with JSON polymorphism for all event types
- Add SseEventParser: resilient async SSE stream reader
- Add ToolCallArgsAccumulator for delta concatenation

Refs: specification/v0_9/docs/a2ui_protocol.md"

# Scopes: dotnet-sdk | avalonia-renderer | avalonia-app | tests | docs | chore | fix
```

---

## A2UI Protocol Quick Reference

Wire format is JSONL — one JSON object per line.

**AG-UI events (28 discriminators):**
Lifecycle: `RUN_STARTED` `RUN_FINISHED` `RUN_ERROR` `STEP_STARTED` `STEP_FINISHED`
Text: `TEXT_MESSAGE_START` `TEXT_MESSAGE_CONTENT` `TEXT_MESSAGE_END` `TEXT_MESSAGE_CHUNK`
Tool: `TOOL_CALL_START` `TOOL_CALL_ARGS` `TOOL_CALL_END` `TOOL_CALL_RESULT` `TOOL_CALL_CHUNK`
State: `STATE_SNAPSHOT` `STATE_DELTA` `MESSAGES_SNAPSHOT` `ACTIVITY_SNAPSHOT` `ACTIVITY_DELTA`
Reasoning: `REASONING_START` `REASONING_MESSAGE_START` `REASONING_MESSAGE_CONTENT` `REASONING_MESSAGE_END` `REASONING_MESSAGE_CHUNK` `REASONING_END` `REASONING_ENCRYPTED_VALUE`
Extension: `RAW` `CUSTOM`

**A2UI messages (server→client):**
`createSurface` `deleteSurface` `updateComponents` `updateDataModel`

**A2UI messages (client→server):**
`action` `error`

**Transport metadata schemas (not first-class messages — flow via A2A/MCP metadata):**
`ServerCapabilities` `ClientCapabilities` `ClientDataModel`

**Catalog types from `specification/v0_9/json/basic_catalog.json` (18 types):**
Display: `Text` `Image` `Icon` `Video` `AudioPlayer` `Divider`
Layout: `Row` `Column` `List` `Card` `Tabs` `Modal`
Interactive: `Button` `TextField` `CheckBox` `ChoicePicker` `DateTimeInput` `Slider`

---

## Next Milestone

**Restaurant Demo Shell** — see `RESTAURANT_DEMO_PORT_PLAN.md` for full plan.
Key decisions: A2A over HTTP transport, v0.8 `userAction` outbound format
(Python agent compatibility), v0.9 inbound messages.

**Gotcha — action wire format:** Python agents read `DataPart.data.userAction`
(v0.8 envelope). Our `ClientToServerMessage` is v0.9 format. For Phase 1,
the Shell must serialize the v0.8 shape. See `samples/client/lit/shell/app.ts:492`
and `samples/agent/adk/restaurant_finder/agent_executor.py:73-85`.

---

## Cross-Reference Implementations

For porting decisions, compare with existing SDK/renderer implementations:
- `agent_sdks/python/src/` — Python SDK (reference)
- `renderers/lit/src/` — Lit renderer (simplest catalog)
- `renderers/angular/src/` — Angular renderer (typed, closest to C#)