@AGENTS.md

# Claude Code Workspace -- Additional Notes

## Claude-Specific Configuration

### Hooks (`.claude/settings.json`)

- **PreToolUse**: Blocks Write/Edit to `specification/` directory (read-only upstream reference)
- **PostToolUse**: Auto-formats `.cs` files with CSharpier after every Write/Edit

### Skills (`.claude/skills/` — thin wrappers delegating to `.github/skills/`)

- **build-and-test**: Build the solution and run tests with summary reporting
- **project-structure**: Explains the A2UI .NET/Avalonia project layout
- **review-spec**: Cross-reference A2UI spec with Lit shell and Python agent implementations

### Formatting note

CSharpier is the authoritative formatter and runs automatically via the PostToolUse hook.
Use `dotnet format analyzers` only for analyzer rule checks -- not whitespace formatting.
The two tools can conflict on whitespace; always let CSharpier have the last word.

### Known Rider / ReSharper false positives

Rider occasionally reports "errors" that the Roslyn compiler does not.
Before spending time "fixing" these, confirm with the build
(`dotnet build --configuration Release`, `TreatWarningsAsErrors=true`).
Known cases:

- **"Invalid markup extension type: expected `X`, actual `CompiledBinding`"** in
  `.axaml` files (`ShellWindow.axaml`, `GalleryWindow.axaml`, any view that sets
  `x:DataType`). Rider does not fully infer the resolved type of Avalonia's
  compile-time-upgraded `{Binding}` / `{CompiledBinding}` markup extensions,
  so it flags every bound `Text`, `IsVisible`, `Surface`, etc. as a type
  mismatch. The Avalonia XAML compiler handles these correctly at build time.
- **"Inconsistent braces style: missing braces"** in C# files such as
  `GalleryViewModel.cs`. The project already enforces
  `csharp_prefer_braces = true:error` (`.editorconfig:368`) and every run of
  `dotnet format style --diagnostics IDE0011 --verify-no-changes` passes clean.
  When Rider reports this in isolation, it is a stale inspection state.

Both issues are noise. Do not rewrite source to appease them; the build
is the source of truth.

## Status Docs

> **Implementation status:** See [`docs/DOTNET_AVALONIA_IMPLEMENTATION.md`](docs/DOTNET_AVALONIA_IMPLEMENTATION.md)
>
> **Telemetry reference:** See [`docs/DOTNET_TELEMETRY_REFERENCE.md`](docs/DOTNET_TELEMETRY_REFERENCE.md)
