---
name: build-and-test
description: How to build, test, and format .NET projects in the A2UI repository.
---

## Build, Test, and Format Commands

Run all commands from the repo root.

```bash
# Build all 8 projects
dotnet build A2Ui.slnx --configuration Release

# Run all tests (487 tests across 3 test projects)
dotnet test A2Ui.slnx --configuration Release --no-build

# Format all C# files (authoritative formatter)
dotnet csharpier format .

# Verify analyzer rules are clean (not whitespace -- CSharpier handles that)
dotnet format analyzers A2Ui.slnx --verify-no-changes
```

## Building and Testing Individual Projects

For isolated changes, build only the affected project and its test project.
Fix issues, then build the full solution before committing.

```bash
# Build a single project
dotnet build agent_sdks/dotnet/src/A2Ui.Core/A2Ui.Core.csproj --configuration Release

# Test a single test project
dotnet test agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui.Core.Tests.csproj --configuration Release

# Run a single test by filter
dotnet test A2Ui.slnx --configuration Release --no-build --filter "FullyQualifiedName~ClassName.MethodName"
```

## Project Map

| Source Project | Test Project | Description |
|----------------|-------------|-------------|
| `agent_sdks/dotnet/src/AgUi.Protocol/` | `agent_sdks/dotnet/tests/AgUi.Protocol.Tests/` | AG-UI 28-event types, SSE parser |
| `agent_sdks/dotnet/src/A2Ui.Core/` | `agent_sdks/dotnet/tests/A2Ui.Core.Tests/` | A2UI messages, SurfaceManager, DataModel |
| `renderers/avalonia/src/A2Ui.Avalonia/` | `renderers/avalonia/tests/A2Ui.Avalonia.Tests/` | Avalonia catalog, renderer, bridge |
| -- | `agent_sdks/dotnet/tests/A2Ui.TestHelpers/` | Shared test utilities (not runnable) |
| `samples/client/avalonia/gallery_v0_9/` | -- | Gallery replay harness (no tests) |
| `samples/client/avalonia/Shell/` | -- | Restaurant demo Shell (no tests) |

## Speed Tips

- Add `--no-restore` after the first build to skip NuGet restore on subsequent builds.
- All projects target `net10.0` only -- no multi-target framework considerations.
