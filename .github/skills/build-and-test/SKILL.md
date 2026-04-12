---
name: build-and-test
description: How to build, test, and format .NET projects in the A2UI repository. Use this when verifying or testing changes.
---

- See `../project-structure/SKILL.md` for project structure details.
- All projects target `net10.0` only -- no multi-target framework considerations.

## Build, Test, and Format Commands

Run all commands from the repo root. Use `--tl:off` when building to avoid terminal flickering in agents.

```bash
# Build all 9 projects
dotnet build A2Ui.slnx --configuration Release --tl:off

# Run all tests (3 test projects, all use Microsoft Testing Platform via xUnit v3)
dotnet test A2Ui.slnx --configuration Release --no-build

# Format all C# files (CSharpier is the authoritative formatter)
dotnet csharpier format .

# Verify analyzer rules are clean (not whitespace -- CSharpier handles that)
dotnet format analyzers A2Ui.slnx --verify-no-changes

# Build/test/format a specific project (preferred for isolated/internal changes)
dotnet build renderers/avalonia/src/A2Ui.Avalonia/A2Ui.Avalonia.csproj --configuration Release --tl:off
dotnet test --project renderers/avalonia/tests/A2Ui.Avalonia.Tests --configuration Release
dotnet csharpier format renderers/avalonia/src/A2Ui.Avalonia/

# Run a single test using MTP filter-query syntax
# Use assembly/namespace/class/method path with * wildcards
dotnet test --project agent_sdks/dotnet/tests/A2Ui.Core.Tests --configuration Release --filter-query "/*/A2Ui.Core.Tests/DataModelTests/Apply_UpdateDataModel_SetsNestedPath"

# Run tests matching a pattern
dotnet test --project agent_sdks/dotnet/tests/A2Ui.Core.Tests --configuration Release --filter-query "/*/*/DataModelTests/*"

# Run tests directly via dotnet run (MTP native command line, bypasses dotnet test)
dotnet run --project agent_sdks/dotnet/tests/A2Ui.Core.Tests --configuration Release

# Show MTP command line help for a test project
dotnet run --project agent_sdks/dotnet/tests/A2Ui.Core.Tests -- -?
```

## Speeding Up Builds and Testing

| Change type | What to do |
|-------------|------------|
| Isolated/Internal logic | Build only the affected project and its test project. Fix issues, then build the full solution and run all tests before committing. |
| Public API surface | Build the full solution and run all tests immediately. |

### Package Restore tip

`dotnet build` restores packages on each build, which can be slow.
Unless packages have changed, add `--no-restore` to skip this step:

```bash
dotnet build A2Ui.slnx --configuration Release --no-restore --tl:off
```

Remember to run `dotnet restore` after pulling changes, modifying project references, or building for the first time.

## Microsoft Testing Platform (MTP)

Tests use the [Microsoft Testing Platform](https://learn.microsoft.com/dotnet/core/testing/unit-testing-platform-intro) via xUnit v3. Key differences from the legacy VSTest runner:

- **`dotnet test` requires `--project`** to specify a test project directly (positional arguments are no longer supported).
- **Test output** uses the MTP format (e.g., `Passed! - Failed: 0, Passed: 108, Skipped: 0`).
- **Test filtering** uses `--filter-query` with `/<assembly>/<namespace>/<class>/<method>` path syntax and `*` wildcards, not the VSTest `--filter "FullyQualifiedName~..."` syntax.
- **TRX reports** use `--report-xunit-trx` instead of `--logger trx`.
- **Running a test project directly** via `dotnet run --project <test-project>` bypasses `dotnet test` and runs the test executable with the MTP command line.
- **Exit code 8** means "no tests matched the filter" -- not a real failure. Use `--ignore-exit-code 8` when running filtered tests across the solution to suppress this.

## Project Map

| Source Project | Test Project | Description |
|----------------|-------------|-------------|
| `agent_sdks/dotnet/src/AgUi.Protocol/` | `agent_sdks/dotnet/tests/AgUi.Protocol.Tests/` | AG-UI 28-event types, SSE parser |
| `agent_sdks/dotnet/src/A2Ui.Core/` | `agent_sdks/dotnet/tests/A2Ui.Core.Tests/` | A2UI messages, SurfaceManager, DataModel |
| `renderers/avalonia/src/A2Ui.Avalonia/` | `renderers/avalonia/tests/A2Ui.Avalonia.Tests/` | Avalonia catalog, renderer, bridge |
| -- | `agent_sdks/dotnet/tests/A2Ui.TestHelpers/` | Shared test utilities (not runnable) |
| `samples/client/avalonia/gallery_v0_9/` | -- | Gallery replay harness (no tests) |
| `samples/client/avalonia/Shell/` | -- | Restaurant demo Shell (no tests) |
