# A2UI Local Gallery — Avalonia Desktop

A native Avalonia UI desktop application that renders all v0.9 A2UI spec examples
using the `A2Ui.Avalonia` renderer library.

## Features

- Loads 40 spec JSON examples (7 minimal + 33 basic catalog)
- Three-pane layout: navigation sidebar, preview with message stepper, inspector
- Message stepper: step through A2UI messages one at a time or all at once
- Live data model inspector (pretty-printed JSON)
- Action log: timestamped record of all user interactions in rendered surfaces
- No agent or network required — purely local, filesystem-based

## Build & Run

```bash
cd samples/client/avalonia/gallery_v0_9
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Build
dotnet build --configuration Release

# Run (requires display)
dotnet run --configuration Release
```

## Architecture

- **MVVM** with CommunityToolkit.Mvvm source generators
- **DI** via Microsoft.Extensions.DependencyInjection
- **GalleryDataLoader** reads spec JSON from `Specs/` directory at runtime
- **GalleryViewModel** feeds A2UI messages to `SurfaceManager.Process()`
- **GalleryWindow** code-behind wires `A2UiSurface.Refresh()` and `UserActionFired`
- **App-level styling** via Application.Resources (dark slate theme)
- **Renderer styling** via A2UiDefaultStyles.axaml (loaded by A2UiSurface)

## Adding Custom Examples

Drop any valid A2UI v0.9 JSON file into `bin/Release/net10.0/Specs/minimal/` or
`bin/Release/net10.0/Specs/basic/` — the app picks them up on next launch without
recompilation. JSON format: `{ "name": "...", "description": "...", "messages": [...] }`
