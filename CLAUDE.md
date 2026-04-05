# A2UI .NET/Avalonia Implementation — Claude Code Workspace

## Project Goal

Build the first .NET/C# implementation of the A2UI (Agent-to-UI) protocol,
contributed directly to this fork of `google/A2UI` (Apache 2.0).

Deliverables living inside this repository:
- `agent_sdks/dotnet/` — C# SDK: AG-UI event types + A2UI message model
- `renderers/avalonia/` — Avalonia renderer: catalog registry + control implementations
- `samples/client/avalonia/` — Composer port: native MVVM desktop app

**Fork:** `https://github.com/<your-handle>/A2UI`
**Branch:** `feature/dotnet-avalonia-renderer`
**Host:** DGX Spark `spark-one` (aarch64, Ubuntu 24.04, 128 GB unified memory)
**Inference:** `nemotron-3-super:120b` via Ollama on `localhost:11434`

---

## Repository Structure (actual)

```
A2UI/                                  ← repo root (fork of google/A2UI)
├── CLAUDE.md                          ← you are here
├── .claude/
│   ├── commands/                      ← /slash commands
│   └── skills/                        ← auto-loaded skill instructions
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
│   │       └── basic_catalog.json
│   └── v0_10/                         ← proposed next version (draft, do not implement yet)
│
├── agent_sdks/                        ← SDK implementations per language
│   ├── python/                        ← existing reference implementation
│   ├── java/                          ← existing reference implementation
│   └── dotnet/                        ← OUR NEW CODE
│       ├── src/
│       │   ├── AgUi.Protocol/         ← AG-UI 28-event types + SSE transport
│       │   └── A2Ui.Core/             ← A2UI message model, DynamicValue, SurfaceManager
│       ├── tests/
│       │   ├── AgUi.Protocol.Tests/
│       │   └── A2Ui.Core.Tests/
│       ├── A2Ui.sln
│       ├── global.json
│       ├── Directory.Build.props
│       ├── Directory.Packages.props
│       └── .editorconfig
│
├── renderers/                         ← renderer libraries per platform
│   ├── lit/                           ← existing web renderer
│   ├── angular/                       ← existing Angular renderer
│   ├── web_core/                      ← shared web core
│   ├── markdown/
│   └── avalonia/                      ← OUR NEW CODE
│       ├── src/
│       │   └── A2Ui.Avalonia/        ← catalog registry + 18 v0.9 catalog entries
│       └── tests/
│           └── A2Ui.Avalonia.Tests/  ← Avalonia.Headless.XUnit
│
├── samples/
│   ├── agent/adk/                     ← Python reference agents
│   └── client/
│       ├── lit/                       ← existing web clients
│       ├── angular/                   ← existing Angular clients
│       └── avalonia/                  ← OUR NEW CODE
│           └── composer/              ← port of tools/composer/ to native MVVM
│
└── tools/
    └── composer/                      ← original web Composer (source to study for port)
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
```

Target **v0.9** for implementation. Review `specification/v0_10/` for forward-compatibility
hints but do not implement v0.10 features yet.

---

## Environment

.NET 10 installed via apt — `dotnet` is on PATH at `/usr/bin/dotnet`:

```bash
dotnet --version    # 10.0.x
```

Always set for aarch64 (DGX Spark/Ubuntu 24.04 has no full ICU libraries):

```bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
```

If `dotnet` is not found (installed via script instead of apt):
```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
```

---

## Build & Test Commands

Run from the relevant subdirectory:

```bash
# .NET SDK (agent_sdks/dotnet/)
cd agent_sdks/dotnet
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build A2Ui.sln --configuration Release
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test A2Ui.sln --configuration Release --no-build

# Avalonia renderer (renderers/avalonia/)
cd renderers/avalonia
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build src/A2Ui.Avalonia/A2Ui.Avalonia.csproj --configuration Release
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/A2Ui.Avalonia.Tests/A2Ui.Avalonia.Tests.csproj --configuration Release

# Avalonia app (samples/client/avalonia/composer/)
cd samples/client/avalonia/composer
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release
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
dotnet csharpier agent_sdks/dotnet/src/
dotnet csharpier renderers/avalonia/src/A2Ui.Avalonia/
dotnet csharpier samples/client/avalonia/
```

---

## Git Workflow

```bash
git checkout feature/dotnet-avalonia-renderer

# Commit format — factual, no marketing language (matches repo convention)
git commit -m "feat(dotnet-sdk): implement AG-UI 26-event C# model

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

**Catalog types from `specification/v0_9/json/basic_catalog.json` (18 types):**
Display: `Text` `Image` `Icon` `Video` `AudioPlayer` `Divider`
Layout: `Row` `Column` `List` `Card` `Tabs` `Modal`
Interactive: `Button` `TextField` `CheckBox` `ChoicePicker` `DateTimeInput` `Slider`

---

## Existing Code to Study Before Writing Any .NET

```bash
# 1. Read the spec first
cat specification/v0_9/docs/a2ui_protocol.md

# 2. Python SDK — understand the message model
ls agent_sdks/python/src/

# 3. Lit renderer — simplest catalog pattern
ls renderers/lit/src/

# 4. Angular renderer — typed catalog entries, closest to C# structure
ls renderers/angular/src/

# 5. Composer tool — source for the Avalonia app port
ls tools/composer/

# Flutter renderer is external: https://github.com/flutter/genui
```

---

## Available Slash Commands

| Command | Purpose |
|---------|---------|
| `/env-check` | Verify dotnet, git, Ollama model |
| `/scan-repo` | Read spec files and map existing renderers |
| `/dotnet-build` | Build with error summary |
| `/dotnet-test` | Run tests with pass/fail summary |
| `/coverage` | Generate coverage report |
| `/new-feature` | Scaffold feature branch |

---

## Licenses

| Component | License |
|-----------|---------|
| A2UI spec + this repo | Apache 2.0 |
| AG-UI spec | MIT |
| Avalonia | MIT |
| CommunityToolkit.Mvvm | MIT |
| xUnit | Apache 2.0 |
| FluentAssertions | Apache 2.0 |
| NSubstitute | BSD 3-Clause |
| Coverlet | MIT |
| CSharpier | Apache 2.0 |
| Roslynator | MIT |