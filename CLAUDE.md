@AGENTS.md

# Claude Code Workspace -- Additional Notes

## Claude-Specific Configuration

### Hooks (`.claude/settings.json`)

- **PreToolUse**: Blocks Write/Edit to `specification/` directory (read-only upstream reference)
- **PostToolUse**: Auto-formats `.cs` files with CSharpier after every Write/Edit

### Skills (`.claude/skills/`)

- **dotnet-build**: Build the solution and run tests with summary reporting
- **review-spec**: Cross-reference A2UI spec with Lit shell and Python agent implementations

### Formatting note

CSharpier is the authoritative formatter and runs automatically via the PostToolUse hook.
Use `dotnet format analyzers` only for analyzer rule checks -- not whitespace formatting.
The two tools can conflict on whitespace; always let CSharpier have the last word.

## Status Docs

> **Implementation status:** See [`docs/DOTNET_AVALONIA_IMPLEMENTATION.md`](docs/DOTNET_AVALONIA_IMPLEMENTATION.md)
>
> **Telemetry reference:** See [`docs/DOTNET_TELEMETRY_REFERENCE.md`](docs/DOTNET_TELEMETRY_REFERENCE.md)
