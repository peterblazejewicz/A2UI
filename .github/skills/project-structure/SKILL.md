---
name: project-structure
description: Explains the project structure of the A2UI .NET/Avalonia solution
---

# A2UI .NET/Avalonia Project Structure

```
A2UI/                               <- repo root (fork of google/A2UI)
+-- A2Ui.slnx                       <- solution file (8 projects)
+-- Directory.Build.props            <- shared MSBuild (Nullable, TreatWarningsAsErrors)
+-- Directory.Packages.props         <- central package management
+-- global.json                      <- .NET SDK pin (10.0.x)
|
+-- agent_sdks/dotnet/               <- .NET SDK
|   +-- src/
|   |   +-- AgUi.Protocol/           <- AG-UI event types, SSE parser, tool-call accumulator
|   |   +-- A2Ui.Core/               <- A2UI messages, validation, SurfaceManager, DataModel
|   +-- tests/
|       +-- AgUi.Protocol.Tests/     <- Protocol unit tests
|       +-- A2Ui.Core.Tests/         <- Core unit + telemetry tests
|       +-- A2Ui.TestHelpers/        <- Shared TestLoggerProvider + TestActivityListener
|
+-- renderers/avalonia/              <- Avalonia renderer
|   +-- src/A2Ui.Avalonia/           <- Catalog registry (18 entries), function registry, bridge
|   +-- tests/A2Ui.Avalonia.Tests/   <- Headless UI tests + spec example integration tests
|
+-- samples/client/avalonia/         <- Avalonia sample apps
|   +-- gallery_v0_9/                <- Offline spec-example replay harness
|   +-- Shell/                       <- Restaurant demo A2A client
|
+-- specification/                   <- Protocol specs (read-only reference)
    +-- v0_9/                        <- Current version (target for implementation)
    +-- v0_10/                       <- Next version (draft, do not implement yet)
```

## Main Folders

| Folder | Contents |
|--------|----------|
| `agent_sdks/dotnet/src/` | Source code: protocol events and A2UI message model |
| `agent_sdks/dotnet/tests/` | Test projects and shared test helpers |
| `renderers/avalonia/src/` | Avalonia control catalog and rendering engine |
| `renderers/avalonia/tests/` | Headless UI tests and spec integration tests |
| `samples/client/avalonia/` | Sample Avalonia desktop applications |
| `specification/v0_9/` | Authoritative protocol spec, JSON schemas, catalog examples |
