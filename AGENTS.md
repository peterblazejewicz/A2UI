# A2UI .NET/Avalonia Implementation

Instructions for AI coding agents working in this repository.

## Project Goal

Build the first .NET/C# implementation of the A2UI (Agent-to-UI) protocol,
contributed directly to this fork of `google/A2UI` (Apache 2.0).

Deliverables living inside this repository:
- `agent_sdks/dotnet/` — C# SDK: AG-UI event types + A2UI message model
- `renderers/avalonia/` — Avalonia renderer: catalog registry + control implementations
- `samples/client/avalonia/` — Gallery v0.9 port + Restaurant demo Shell client

**Fork:** `https://github.com/peterblazejewicz/A2UI`
**Host:** Windows 11 Pro (development workstation)

> **Implementation status:** See [`docs/DOTNET_AVALONIA_IMPLEMENTATION.md`](docs/DOTNET_AVALONIA_IMPLEMENTATION.md)
> for the authoritative status tracker — protocol coverage, actor/component map,
> architecture diagrams, design decisions, and review history.
>
> **Telemetry reference:** See [`docs/DOTNET_TELEMETRY_REFERENCE.md`](docs/DOTNET_TELEMETRY_REFERENCE.md)
> for the shipped `ActivitySource` names, `LoggerMessage` EventId ranges,
> BeginScope property keys, and `A2Ui.TestHelpers` usage.

---

## Repository Structure

```
A2UI/                                  <- repo root (fork of google/A2UI)
+-- AGENTS.md                          <- AI agent instructions (you are here)
+-- CLAUDE.md                          <- Claude Code workspace (imports this file)
+-- A2Ui.slnx                          <- solution file (all 8 .NET projects)
+-- global.json                        <- .NET SDK version pin
+-- Directory.Build.props              <- shared MSBuild settings (Nullable, analyzers)
+-- Directory.Packages.props           <- central package management
|
+-- specification/                     <- A2UI protocol specs (read-only reference)
|   +-- v0_8/                          <- stable
|   +-- v0_9/                          <- current working version (use this)
|   |   +-- docs/a2ui_protocol.md      <- authoritative spec doc
|   |   +-- json/                      <- server_to_client.json, basic_catalog.json, ...
|   +-- v0_10/                         <- proposed next version (draft, do not implement yet)
|
+-- agent_sdks/                        <- SDK implementations per language
|   +-- python/                        <- existing reference implementation
|   +-- java/                          <- existing reference implementation
|   +-- dotnet/                        <- OUR .NET/C# CODE
|       +-- src/
|       |   +-- AgUi.Protocol/         <- AG-UI 28-event types, SSE parser, tool-call accumulator
|       |   +-- A2Ui.Core/             <- A2UI messages, validation, SurfaceManager, DataModel
|       +-- tests/
|           +-- AgUi.Protocol.Tests/   <- xUnit v3 + MTP (Exe)
|           +-- A2Ui.Core.Tests/       <- xUnit v3 + MTP (Exe)
|           +-- A2Ui.TestHelpers/      <- shared TestLoggerProvider + TestActivityListener
|
+-- renderers/                         <- renderer libraries per platform
|   +-- lit/                           <- existing web renderer (reference-only for .NET port)
|   +-- angular/                       <- existing Angular renderer
|   +-- web_core/                      <- shared web core
|   +-- avalonia/                      <- OUR AVALONIA RENDERER
|       +-- src/A2Ui.Avalonia/         <- catalog registry, 20 entries, function registry, bridge
|       +-- tests/A2Ui.Avalonia.Tests/ <- Avalonia.Headless.XUnit v2 + headless integration tests
|
+-- samples/
|   +-- agent/adk/                     <- Python reference agents (restaurant_finder is the target)
|   +-- client/
|       +-- lit/                       <- reference-only (port source for Shell)
|       +-- angular/                   <- reference-only
|       +-- avalonia/                  <- OUR NEW CODE
|           +-- gallery_v0_9/          <- offline spec-example replay harness
|           +-- Shell/                 <- Restaurant demo A2A client
|
+-- tools/
    +-- composer/                      <- original web Composer (upstream, not ported)
```

---

## Spec Reference Paths

Always read the spec from the repo -- do not rely on training data:

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

.NET 10 -- `dotnet` is on PATH:

```bash
dotnet --version    # 10.0.x
```

Optional environment variables:

```bash
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
```

---

## Build, Test & Format Commands

Run from the repo root:

```bash
# Build everything (9 projects)
dotnet build A2Ui.slnx --configuration Release

# Test everything (487 tests -- SDK tests use MTP, Avalonia uses VSTest)
dotnet test A2Ui.slnx --configuration Release --no-build

# Test individual projects (MTP-native for SDK tests)
dotnet test --project agent_sdks/dotnet/tests/AgUi.Protocol.Tests --configuration Release --no-build
dotnet test --project agent_sdks/dotnet/tests/A2Ui.Core.Tests --configuration Release --no-build
dotnet test --project renderers/avalonia/tests/A2Ui.Avalonia.Tests --configuration Release --no-build

# Format all C# files
dotnet csharpier format .

# Verify analyzer rules are clean (no whitespace -- CSharpier handles that)
dotnet format analyzers A2Ui.slnx --verify-no-changes

# Build/test individual projects if needed
dotnet build renderers/avalonia/src/A2Ui.Avalonia/A2Ui.Avalonia.csproj --configuration Release
dotnet build samples/client/avalonia/Shell/A2Ui.Avalonia.Shell.csproj --configuration Release

# Run a single test by filter (MTP syntax for SDK tests)
dotnet test --project agent_sdks/dotnet/tests/A2Ui.Core.Tests --configuration Release --no-build --filter-method "*.ClassName.MethodName*"

# Run a single test by filter (VSTest syntax for Avalonia tests)
dotnet test --project renderers/avalonia/tests/A2Ui.Avalonia.Tests --configuration Release --no-build --filter "FullyQualifiedName~ClassName.MethodName"
```

### Speed tips

- Add `--no-restore` to `dotnet build` after the first build (skips NuGet restore).
- Build only the affected project when working on isolated changes, then build the full solution before committing.

---

## Coding Standards

### Non-negotiable rules

- `Nullable` **enabled** everywhere -- zero `!` suppressions without a comment
- `TreatWarningsAsErrors` **true** -- fix warnings, never suppress
- **File-scoped namespaces** -- `namespace Foo.Bar;` not `namespace Foo.Bar { }`
- `var` only when the type is obvious from the right-hand side
- No `async void` except event handlers -- always try/catch inside
- `ConfigureAwait(false)` on all library awaits; `ConfigureAwait(true)` in ViewModels
- Records for immutable data (`sealed record`), classes for mutable/service types
- `CancellationToken` threaded through every async method signature
- Private classes should be `sealed` unless designed for inheritance
- **Encoding**: All new `.cs` files must be UTF-8 with BOM (required by `dotnet format`)

### Naming

| Thing | Convention |
|-------|-----------|
| Private fields | `_camelCase` |
| Static fields | `s_camelCase` |
| Constants | `PascalCase` |
| Interfaces | `IFoo` |
| Async methods | `FooAsync` |
| Test methods | `MethodName_StateUnderTest_ExpectedBehavior` |

### Formatting

```bash
dotnet csharpier format .
```

CSharpier is the authoritative formatter. Use `dotnet format analyzers` only for
analyzer rule checks (not whitespace -- CSharpier handles that).

### Key Design Principles

- **DRY**: Move common logic into helper methods or helper classes
- **Single Responsibility**: Each class should have one clear responsibility
- **Encapsulation**: Keep implementation details private, expose only necessary public APIs
- **Strong Typing**: Use strong types for self-documenting code and compile-time error detection

---

## Core Types

### AG-UI Protocol (`AgUi.Protocol`)

| Type | Purpose |
|------|---------|
| `BaseEvent` | Abstract base with JSON polymorphism for 28 AG-UI event types |
| `SseEventParser` | Resilient async SSE stream reader |
| `ToolCallArgsAccumulator` | Concatenates delta fragments into complete tool-call args |
| `RunAgentInput` | Input model for starting an agent run |

### A2UI Messages (`A2Ui.Core`)

| Type | Purpose |
|------|---------|
| `A2UiMessage` | Top-level protocol message (createSurface, updateComponents, etc.) |
| `A2UiComponent` | Single UI component with type, properties, children |
| `Surface` | Live surface state: components dictionary + data model |
| `DataModel` | JSON Pointer-based data store for two-way binding |
| `SurfaceManager` | Processes A2UI messages, manages surface lifecycle |
| `DynamicValue` | Union type: string literal, path reference, function call, or array |
| `ChildList` | Children as ID array or template (for data-driven repeating) |

### Avalonia Renderer (`A2Ui.Avalonia`)

| Type | Purpose |
|------|---------|
| `ICatalogEntry` | Factory contract for a component type (Create + Update) |
| `CatalogRegistry` | Maps component type strings to `ICatalogEntry` instances |
| `A2UiRenderer` | Renders a `Surface` to Avalonia controls via the catalog |
| `IRenderContext` | Cross-cutting: child rendering, action firing, data model access |
| `AgentEventBridge` | Bridges AG-UI event stream to SurfaceManager processing |
| `FunctionRegistry` | Evaluates DynamicValue function calls (eq, gt, and, or, formatDate, etc.) |

---

## Testing Conventions

- **Framework**: xUnit v3.2.2 (SDK tests) / xUnit v2.9.2 (Avalonia tests)
- **Runner**: Microsoft Testing Platform (SDK) / VSTest bridge (Avalonia)
- **Assertions**: xUnit v3 native `Assert.*` (SDK tests) / FluentAssertions 7.0.0 (Avalonia tests)
- **UI tests**: Avalonia.Headless.XUnit v11.2.0 (headless rendering, no window needed)
- **Shared helpers**: `A2Ui.TestHelpers` provides `TestLoggerProvider` and `TestActivityListener`
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior`
- **Async tests**: Must use `Async` suffix on test method names
- Test methods returning `Task`/`ValueTask` must be async; use `[Fact]` or `[AvaloniaFact]`
- SDK tests use `Assert.Equal(expected, actual)` argument order (expected first)
- Add Arrange/Act/Assert comments for complex tests

---

## Git Workflow

```bash
# Commit format -- factual, no marketing language (matches repo convention)
git commit -m "feat(dotnet-sdk): implement AG-UI 28-event C# model

- Add BaseEvent with JSON polymorphism for all event types
- Add SseEventParser: resilient async SSE stream reader
- Add ToolCallArgsAccumulator for delta concatenation

Refs: specification/v0_9/docs/a2ui_protocol.md"

# Scopes: dotnet-sdk | avalonia-renderer | avalonia-app | tests | docs | chore | fix
```

---

## Cross-Reference Implementations

For porting decisions, compare with existing SDK/renderer implementations:
- `agent_sdks/python/src/` -- Python SDK (reference)
- `renderers/lit/src/` -- Lit renderer (simplest catalog)
- `renderers/angular/src/` -- Angular renderer (typed, closest to C#)

---

## MCP Tools (External Knowledge)

This repository restricts MCP servers to four .NET-relevant sources.
GitHub is **read-only** -- all write tools are denied at the project level.
Use the right tool for the right job; prefer local files and `git log` when
they answer the question faster.

### When to use what

| Context | Tool(s) | Example |
|---------|---------|---------|
| **.NET / C# API lookup** | `microsoft_docs_search`, `microsoft_docs_fetch` | "How does `JsonDerivedType` work in .NET 10?" |
| **C# code samples** | `microsoft_code_sample_search` | "Show me `System.Text.Json` polymorphic serialization" |
| **Library docs (Avalonia, xUnit, FluentAssertions, NSubstitute, Bogus)** | `context7` `resolve-library-id` → `query-docs` | "What's the Avalonia `Headless` test API?" |
| **Upstream repo check** | `github` `search_code`, `get_file_contents` | "Does `google/A2UI` define a v0.10 catalog schema?" |
| **Issue / PR context** | `github` `issue_read`, `pull_request_read`, `search_issues` | "What does google/A2UI#42 say about action payloads?" |
| **Branch & release tracking** | `github` `list_branches`, `list_tags`, `list_releases` | "What's the latest tag on upstream?" |
| **Architecture diagrams** | `mermaid_chart` `validate_and_render_mermaid_diagram` | Render a component-flow diagram for docs |

### Microsoft Learn (`microsoft-learn`)

All three tools available. Use for:
- .NET runtime / BCL API verification (e.g., `ConfigureAwait`, `JsonSerializer` options)
- MSBuild / SDK behavior (e.g., `Directory.Build.props`, central package management)
- Avalonia is **not** on Microsoft Learn -- use Context7 for Avalonia docs

```
microsoft_docs_search   → quick overview (up to 10 chunks, 500 tokens each)
microsoft_code_sample_search → code snippets (up to 20, filterable by language)
microsoft_docs_fetch    → full page as markdown (use after search for depth)
```

### Context7 (`context7`)

All tools available. Use for:
- Avalonia UI framework docs (controls, styling, headless testing)
- xUnit, FluentAssertions, NSubstitute, Bogus API reference
- Any NuGet library not covered by Microsoft Learn

```
resolve-library-id  → get the library ID first
query-docs          → then fetch relevant documentation
```

### GitHub (`github`) -- read-only

Write tools are denied. Use for:
- **Upstream verification**: check `google/A2UI` for spec changes, new issues, or PRs
  that affect our implementation
- **Fork status**: compare branches, check commit history on `peterblazejewicz/A2UI`
- **Code search**: find patterns across the upstream repo (Python SDK, Lit renderer, etc.)

Available read tools: `get_commit`, `get_file_contents`, `get_label`,
`get_latest_release`, `get_release_by_tag`, `get_tag`,
`get_team_members`, `get_teams`, `get_me`, `get_copilot_job_status`,
`issue_read`, `pull_request_read`, `list_branches`, `list_commits`,
`list_issues`, `list_issue_types`, `list_pull_requests`, `list_tags`,
`list_releases`, `search_code`, `search_issues`,
`search_pull_requests`, `search_repositories`, `search_users`.

### Mermaid Chart (`mermaid-chart`)

Single tool: `validate_and_render_mermaid_diagram` (connection can be flaky). Use for:
- Rendering architecture diagrams for documentation
- Validating Mermaid syntax before committing to markdown files

---

## Current State

**Restaurant Demo Shell + Phase 1/2 telemetry shipped** (merged into `feature/dotnet-avalonia-renderer`).
Scaffold, structured logging (10 `LoggerMessage` categories), `ActivitySource`
tracing (4 sources, 7 span names), Serilog `BeginScope` correlation, raw HTTP
handler + `RequestSummary` one-liner, and `A2Ui.TestHelpers` library with
representative scenario tests are all in place. Build is clean, 487/487 tests
pass. See `RESTAURANT_DEMO_PORT_PLAN.md` for the porting status table and
`docs/DOTNET_TELEMETRY_REFERENCE.md` for the telemetry contract.

**Testing platform:** SDK test projects (AgUi.Protocol.Tests, A2Ui.Core.Tests)
migrated to xUnit v3.2.2 + Microsoft Testing Platform. FluentAssertions replaced
with xUnit v3 native `Assert.*`. Avalonia tests remain on xUnit v2 +
FluentAssertions (blocked on Avalonia 12 upgrade for headless xUnit v3 support).
Central Package Management enabled in `Directory.Packages.props`.

**Pending:** manual end-to-end verification run (Python agent + .NET Shell +
Restaurant scenario). Optional Phase 2 work: .NET agent port via MS Agent
Framework + Ollama. Optional Phase 3: OpenTelemetry export.

**Gotcha -- action wire format:** Python agents read `DataPart.data.userAction`
(v0.8 envelope). `ClientToServerMessage` in `A2Ui.Core` is v0.9 format. The
Shell's `UserActionSerializer` emits the v0.8 shape to stay compatible with
the Python agent; `A2Ui.Core` stays v0.9-pure. See
`samples/client/lit/shell/app.ts:492` and
`samples/agent/adk/restaurant_finder/agent_executor.py:73-85` for the
asymmetry.
