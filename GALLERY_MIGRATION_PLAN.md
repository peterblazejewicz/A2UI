# Gallery v0.9 Migration Plan — .NET Avalonia UI Desktop App

## Overview

Port the A2UI v0.9 local gallery (`samples/client/lit/gallery_v0_9/`) to a native
Avalonia UI MVVM desktop application. The gallery loads static spec JSON examples,
feeds them through `SurfaceManager`, and renders them via the `A2Ui.Avalonia`
renderer. No agent, network, or LLM infrastructure required.

**Source reference:** `samples/client/lit/gallery_v0_9/` (Lit/TypeScript web app)
**Target location:** `samples/client/avalonia/gallery_v0_9/`
**Renderer library:** `renderers/avalonia/src/A2Ui.Avalonia/` (18 catalog entries, already built)

---

## Prerequisites

Before starting this plan, verify:

```bash
# SDK tests pass
cd agent_sdks/dotnet
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test A2Ui.sln --configuration Release

# Renderer tests pass
cd renderers/avalonia
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/A2Ui.Avalonia.Tests/A2Ui.Avalonia.Tests.csproj --configuration Release
```

**Required library state (all implemented):**
- `AgUi.Protocol` — 28 event types, `SseEventParser`, `ToolCallArgsAccumulator`
- `A2Ui.Core` — `DynamicValue`, `ChildList`, `SurfaceManager` (events outside lock), `DataModel` (array paths)
- `A2Ui.Avalonia` — 18 catalog entries, `A2UiRenderer` (per-surface cache), `A2UiSurface` (`Refresh()`), `AgentEventBridge` (`UserActionReceived`)

---

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                   A2Ui.Avalonia.Gallery                       │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐    │
│  │ GalleryWindow.axaml — Three-Pane Layout              │    │
│  │                                                      │    │
│  │  ┌──────────┐  ┌─────────────────┐  ┌────────────┐  │    │
│  │  │ Nav      │  │ Preview         │  │ Inspector  │  │    │
│  │  │ Sidebar  │  │ ┌─────────────┐ │  │ ┌────────┐ │  │    │
│  │  │ (ListBox │  │ │ Stepper     │ │  │ │ Data   │ │  │    │
│  │  │  of Demo │  │ │ Controls    │ │  │ │ Model  │ │  │    │
│  │  │  Items)  │  │ └─────────────┘ │  │ │ JSON   │ │  │    │
│  │  │          │  │ ┌─────────────┐ │  │ └────────┘ │  │    │
│  │  │          │  │ │ A2UiSurface │ │  │ ┌────────┐ │  │    │
│  │  │          │  │ │ (Renderer)  │ │  │ │ Action │ │  │    │
│  │  │          │  │ └─────────────┘ │  │ │ Logs   │ │  │    │
│  │  └──────────┘  └─────────────────┘  │ └────────┘ │  │    │
│  │                                      └────────────┘  │    │
│  └──────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌──────────────┐  ┌──────────────────┐                      │
│  │ GalleryVM    │  │ GalleryData      │                      │
│  │ (Commands,   │  │ Loader           │                      │
│  │  State,      │  │ (Filesystem      │                      │
│  │  SurfaceMgr) │  │  JSON reader)    │                      │
│  └──────────────┘  └──────────────────┘                      │
└──────────────────────────────────────────────────────────────┘
         │                    │
         ▼                    ▼
   A2Ui.Avalonia          A2Ui.Core
   (Catalog,Render)       (Surface,DataModel)
```

**Key principle:** No `AgUi.Protocol` dependency. No agents. JSON files are read
from the filesystem at runtime and fed directly to `SurfaceManager.Process()`.

---

## Styling Architecture

The A2UI protocol separates structure from presentation:

- **Protocol layer** — sends only `primaryColor`, `iconUrl`, `agentDisplayName`
  as branding hints via `createSurface.theme`. Components carry semantic `variant`
  hints (`h1`, `primary`), never visual styling.
- **Renderer layer** — provides platform-native defaults, like a browser renders
  `<button>` and `<h1>` with default styles. The renderer is white-label/generic.
- **Application layer** — owns the full visual design. Applies app-specific
  styles at consumption time.

**For this plan:**

| Layer | What we do |
|-------|-----------|
| Renderer (`A2Ui.Avalonia`) | Fix unstyled class name hooks: add Styles for `Heading1`–`Heading5`, `Body`, `Caption`, `Card`, verify `accent`/`danger` Button classes. Platform-native Fluent defaults only. |
| Gallery app | Dark slate shell styling for nav/preview/inspector panes. Demonstrates how a consumer applies its own visual identity. |
| Deferred | Formal `ITheme` injection contract, `primaryColor` interpretation, gradient effects. |

**Ref:** spec `specification/v0_9/docs/a2ui_protocol.md` lines 700–714 (Theme section).

---

## MVVM Architecture + Dependency Injection

The gallery app follows the **CommunityToolkit.Mvvm** pattern aligned with
.NET MAUI, WPF, and WinUI conventions. The same concepts, same ideas, same
underlying architecture — so developers moving between MAUI and Avalonia
find familiar patterns.

### CommunityToolkit.Mvvm

All ViewModels inherit from `ObservableObject` and use source generators:

| Feature | Usage |
|---------|-------|
| `[ObservableProperty]` | Auto-generates `PropertyChanged` notification for private fields |
| `[RelayCommand]` | Auto-generates `ICommand` from methods, supports `CanExecute` |
| `[RelayCommand(CanExecute = ...)]` | Binds command availability to a boolean property |
| `ObservableObject` | Base class for all ViewModels |

This is identical to how MAUI ViewModels are built. No custom INPC boilerplate.

### Dependency Injection

Use `Microsoft.Extensions.DependencyInjection` for service registration and
resolution, matching the MAUI `MauiProgram.CreateMauiApp()` pattern:

```csharp
// Program.cs — service registration
public static AppBuilder BuildAvaloniaApp()
{
    var services = new ServiceCollection();
    services.AddSingleton<GalleryViewModel>();
    services.AddSingleton<SurfaceManager>();
    services.AddTransient<GalleryDataLoader>();

    var provider = services.BuildServiceProvider();
    App.Services = provider;

    return AppBuilder.Configure<App>()
                     .UsePlatformDetect()
                     .WithInterFont()
                     .LogToTrace();
}
```

```csharp
// App.axaml.cs — service locator for window creation
public sealed partial class App : Application
{
    public static IServiceProvider Services { get; set; } = null!;

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new GalleryWindow
            {
                DataContext = Services.GetRequiredService<GalleryViewModel>()
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

**Service lifetime guidelines (same as MAUI):**

| Lifetime | Use for |
|----------|---------|
| `Singleton` | `SurfaceManager`, `GalleryViewModel` (one instance for app lifetime) |
| `Transient` | `GalleryDataLoader` (stateless, create-use-discard) |
| `Scoped` | Not used in this sample (no per-request scope in desktop apps) |

### Dependencies

| Package | Purpose | Alignment |
|---------|---------|-----------|
| `CommunityToolkit.Mvvm` 8.2.0 | MVVM source generators | Same as MAUI, WPF, WinUI |
| `Microsoft.Extensions.DependencyInjection` | DI container | Same as MAUI, ASP.NET Core |
| `Avalonia` 11.2.0 | UI framework | — |
| `Avalonia.Desktop` 11.2.0 | Desktop lifetime | — |
| `Avalonia.Themes.Fluent` 11.2.0 | Fluent Design theme | — |
| `Avalonia.Fonts.Inter` 11.2.0 | Inter font family | — |

---

## Coding Standards (from CLAUDE.md)

- `Nullable` enabled, `TreatWarningsAsErrors` true
- File-scoped namespaces: `namespace X.Y;`
- `sealed record` for immutable data, `sealed class` for services
- `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm
- `ConfigureAwait(false)` in library code, `ConfigureAwait(true)` in ViewModel commands
- `CancellationToken` threaded through all async signatures
- Constructor injection for all dependencies (no `new` in ViewModels for services)
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Commit format: `feat(scope): <description>`

---

## Step 0: Preparatory Cleanup

**Goal:** Remove the old Composer migration plan and update CLAUDE.md to reflect
the gallery-first approach. Avoid confusion in future work.

**Actions:**
1. Delete `COMPOSER_MIGRATION_PLAN.md` from repo root
2. Update `CLAUDE.md`:
   - Repository Structure: add `gallery_v0_9/` under `samples/client/avalonia/`,
     annotate `composer/` as "placeholder scaffold for future agent-connected app (no source code yet)"
   - Build & Test section: replace composer build commands with gallery commands
   - "Existing Code to Study" section: replace composer reference with gallery reference
3. Leave `samples/client/avalonia/composer/` directory untouched (valid csproj scaffold for future use)

### Verification

```bash
# Old plan is gone
test ! -f COMPOSER_MIGRATION_PLAN.md

# CLAUDE.md references gallery
grep -q "gallery_v0_9" CLAUDE.md
```

### Commit
```
chore(docs): replace Composer migration plan with Gallery v0.9 plan

- Delete COMPOSER_MIGRATION_PLAN.md
- Update CLAUDE.md: add gallery_v0_9 to repo structure, update build commands
- Annotate composer/ as placeholder scaffold for future agent app
- Add GALLERY_MIGRATION_PLAN.md
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 1: SDK Addition — DataModel.ToJson()

**Goal:** Expose `DataModel` root state as JSON string for the inspector panel.

**File to modify:**
```
agent_sdks/dotnet/src/A2Ui.Core/DataModel.cs
```

### Implementation

Add one public method to `DataModel`:

```csharp
/// <summary>Serialize the current data model state to a JSON string.</summary>
public string ToJson(bool indented = false) =>
    _root.ToJsonString(new JsonSerializerOptions { WriteIndented = indented });
```

### Test

In `agent_sdks/dotnet/tests/A2Ui.Core.Tests/DataModelTests.cs`, add:

- `ToJson_EmptyModel_ReturnsEmptyObject`
- `ToJson_AfterApply_ReflectsState`
- `ToJson_Indented_ProducesFormattedOutput`

### Verification

```bash
cd agent_sdks/dotnet
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test A2Ui.sln --configuration Release
```

### Commit
```
feat(dotnet-sdk): add DataModel.ToJson() for state serialization

- Expose _root as JSON string via ToJson(bool indented)
- xUnit tests for empty, populated, and indented output
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 2: Renderer Fix — Back Unstyled Class Name Hooks

**Goal:** The Avalonia renderer emits CSS-like class names (`Heading1`, `Card`,
`accent`, etc.) on controls but has no Styles backing them. Fix this by adding
platform-native Fluent-compatible default styles.

**File to create:**
```
renderers/avalonia/src/A2Ui.Avalonia/Themes/A2UiDefaultStyles.axaml
```

### Styles to define

| Class | Target | Style |
|-------|--------|-------|
| `Heading1` | `TextBlock` | FontSize 32, FontWeight Bold |
| `Heading2` | `TextBlock` | FontSize 26, FontWeight SemiBold |
| `Heading3` | `TextBlock` | FontSize 22, FontWeight SemiBold |
| `Heading4` | `TextBlock` | FontSize 18, FontWeight Medium |
| `Heading5` | `TextBlock` | FontSize 15, FontWeight Medium |
| `Body` | `TextBlock` | FontSize 14, FontWeight Normal |
| `Caption` | `TextBlock` | FontSize 12, FontWeight Normal, Opacity 0.7 |
| `Card` | `Border` | Background `{DynamicResource CardBackgroundFillColorDefaultBrush}`, BorderBrush `{DynamicResource CardStrokeColorDefaultBrush}`, BorderThickness 1, CornerRadius 8, Padding 16 |

Verify that `accent` and `danger` on `Button` already work with FluentTheme
(Fluent defines `accent` natively). If `danger` is missing, add a Style for it.

### Integration

Load from `A2UiSurface` constructor so it works out of the box. The styles
AXAML is added as an `AvaloniaResource` in `A2Ui.Avalonia.csproj` and
`A2UiSurface` includes it in its `Styles` collection during construction:

```csharp
Styles.Add(new StyleInclude(new Uri("avares://A2Ui.Avalonia"))
{
    Source = new Uri("avares://A2Ui.Avalonia/Themes/A2UiDefaultStyles.axaml")
});
```

Consuming apps can override any style via normal Avalonia style precedence.
Because `A2UiSurface` loads its styles at the control level, application-level
styles declared in `App.axaml` take precedence automatically.

#### Overriding renderer defaults — example

To customize `Heading1` in a consuming application, add a style in `App.axaml`
that targets the same class:

```xml
<!-- App.axaml — application-level style overrides renderer defaults -->
<Application.Styles>
  <FluentTheme />

  <!-- Override the renderer's Heading1 style with a custom brand font -->
  <Style Selector="TextBlock.Heading1">
    <Setter Property="FontSize" Value="36" />
    <Setter Property="FontWeight" Value="Black" />
    <Setter Property="Foreground" Value="{DynamicResource AccentFillColorDefaultBrush}" />
  </Style>

  <!-- Override Card background for dark branded surfaces -->
  <Style Selector="Border.Card">
    <Setter Property="Background" Value="#1a1a2e" />
    <Setter Property="BorderBrush" Value="#16213e" />
  </Style>
</Application.Styles>
```

Avalonia resolves styles by specificity and declaration order. Application-level
styles (in `App.axaml`) override control-level styles (loaded by `A2UiSurface`).
This follows the same pattern as MAUI's implicit styles and `ResourceDictionary`
merge hierarchy.

To completely replace the renderer's default theme, set
`A2UiSurface.Styles.Clear()` in code and load your own `StyleInclude`:

```csharp
var surface = new A2UiSurface();
surface.Styles.Clear(); // Remove renderer defaults
surface.Styles.Add(new StyleInclude(new Uri("avares://MyApp"))
{
    Source = new Uri("avares://MyApp/Themes/MyBrandTheme.axaml")
});
```

### Tests

In `A2Ui.Avalonia.Tests`, add:
- `TextCatalogEntry_Create_WithHeading1Variant_HasHeading1Class` (verify existing)
- `A2UiDefaultStyles_Heading1_SetsFontSize32` (headless style resolution test)

### Verification

```bash
cd renderers/avalonia
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/A2Ui.Avalonia.Tests/A2Ui.Avalonia.Tests.csproj --configuration Release
```

### Commit
```
fix(avalonia-renderer): add default Styles for component class name hooks

- A2UiDefaultStyles.axaml: Heading1-5, Body, Caption, Card styles
- Fluent-compatible defaults, overridable by consuming apps
- Loaded by A2UiSurface for out-of-box usability
- Tests for style resolution
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 3: Gallery App Scaffold

**Goal:** A buildable, launchable Avalonia desktop app with an empty window.

**Files to create:**
```
samples/client/avalonia/gallery_v0_9/
├── A2Ui.Avalonia.Gallery.csproj
├── Program.cs
├── App.axaml
├── App.axaml.cs
└── Views/
    └── GalleryWindow.axaml
    └── GalleryWindow.axaml.cs
```

### A2Ui.Avalonia.Gallery.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AssemblyName>A2Ui.Avalonia.Gallery</AssemblyName>
    <RootNamespace>A2Ui.Avalonia.Gallery</RootNamespace>
    <UseAvalonia>true</UseAvalonia>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.2.0" />
    <PackageReference Include="Avalonia.Desktop" Version="11.2.0" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.2.0" />
    <PackageReference Include="Avalonia.Fonts.Inter" Version="11.2.0" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
    <PackageReference Include="Avalonia.Diagnostics" Version="11.2.0">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../../../renderers/avalonia/src/A2Ui.Avalonia/A2Ui.Avalonia.csproj" />
  </ItemGroup>

  <!-- Spec JSON files: linked from specification/, copied to output at build -->
  <ItemGroup>
    <Content Include="..\..\..\..\specification\v0_9\json\catalogs\minimal\examples\*.json"
             Link="Specs\minimal\%(Filename)%(Extension)"
             CopyToOutputDirectory="PreserveNewest" />
    <Content Include="..\..\..\..\specification\v0_9\json\catalogs\basic\examples\*.json"
             Link="Specs\basic\%(Filename)%(Extension)"
             CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

**Key decisions:**
- `Avalonia.Themes.Fluent` (not `Default` — matches renderer)
- `TreatWarningsAsErrors` true (CLAUDE.md non-negotiable)
- `Microsoft.Extensions.DependencyInjection` for DI (same as MAUI)
- `CommunityToolkit.Mvvm` for MVVM source generators (same as MAUI/WPF/WinUI)
- Only references `A2Ui.Avalonia` (no `AgUi.Protocol`, no `A2Ui.Core` direct ref — transitive)
- Spec JSON as `Content` with `CopyToOutputDirectory` — runtime filesystem loading

### Program.cs

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace A2Ui.Avalonia.Gallery;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        App.Services = services.BuildServiceProvider();

        return AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .WithInterFont()
                         .LogToTrace();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Core services
        services.AddSingleton<SurfaceManager>();

        // Data loading
        services.AddTransient<GalleryDataLoader>();

        // ViewModels
        services.AddSingleton<GalleryViewModel>();
    }
}
```

### App.axaml

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="A2Ui.Avalonia.Gallery.App"
             RequestedThemeVariant="Dark">
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>
</Application>
```

### App.axaml.cs

```csharp
namespace A2Ui.Avalonia.Gallery;

public sealed partial class App : Application
{
    public static IServiceProvider Services { get; set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.GalleryWindow
            {
                DataContext = Services.GetRequiredService<GalleryViewModel>()
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

### GalleryWindow.axaml

Minimal stub — title bar with "A2UI Local Gallery — v0.9" text to prove it launches.

### Verification

```bash
cd samples/client/avalonia/gallery_v0_9
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release
```

### Commit
```
feat(avalonia-app): scaffold Gallery v0.9 desktop app

- Program.cs with Avalonia desktop lifetime
- App.axaml with FluentTheme (Dark)
- Empty GalleryWindow proving the app builds
- Spec JSON files linked as Content from specification/
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 4: DemoItem Model + GalleryDataLoader

**Goal:** Load all 40 spec JSON examples from the filesystem into typed records.

**Files to create:**
```
samples/client/avalonia/gallery_v0_9/
├── Models/
│   └── DemoItem.cs
└── Services/
    └── GalleryDataLoader.cs
```

### DemoItem.cs

```csharp
namespace A2Ui.Avalonia.Gallery.Models;

/// <summary>A single gallery example with its A2UI messages.</summary>
public sealed record DemoItem(
    string Id,
    string Title,
    string Filename,
    string Description,
    A2UiMessage[] Messages,
    bool IsBasic);
```

### GalleryDataLoader.cs

```csharp
namespace A2Ui.Avalonia.Gallery.Services;

/// <summary>
/// Loads A2UI spec examples from Specs/ directory relative to the app executable.
/// Handles both envelope format ({ name, description, messages[] }) and bare array format.
/// Auto-injects a createSurface message if the example doesn't include one.
/// </summary>
public static class GalleryDataLoader
{
    public static async Task<IReadOnlyList<DemoItem>> LoadAsync(CancellationToken ct = default)
    {
        // ...
    }
}
```

**Implementation details:**
- Base path: `Path.Combine(AppContext.BaseDirectory, "Specs")`
- Scans `Specs/minimal/*.json` and `Specs/basic/*.json` via `Directory.GetFiles("*.json")`
- For each file: `File.ReadAllTextAsync` → `JsonDocument.Parse` → extract messages
- If root is array → treat as bare messages; if object → read `.messages` property
- If no `createSurface` message found → prepend synthetic one with `surfaceId` from filename
  and `catalogId` = `"basic"` or `"minimal"` based on directory
- Title derived from filename: split on `_` or `-`, capitalize, strip `.json`
- Sort: minimal examples first (by filename), then basic examples (by filename)

### Verification

Build succeeds. Load logic will be tested via the ViewModel in Step 5.

### Commit
```
feat(avalonia-app): add DemoItem model and GalleryDataLoader

- DemoItem sealed record with Id, Title, Messages
- GalleryDataLoader: filesystem JSON reader with envelope/array format support
- Auto-injects createSurface when missing (matches lit gallery behavior)
- Title derivation from filename
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 5: GalleryViewModel

**Goal:** Core ViewModel with commands, SurfaceManager wiring, and action logging.

**File to create:**
```
samples/client/avalonia/gallery_v0_9/
└── ViewModels/
    └── GalleryViewModel.cs
```

### Key elements

```csharp
namespace A2Ui.Avalonia.Gallery.ViewModels;

public sealed partial class GalleryViewModel : ObservableObject, IDisposable
{
    private readonly SurfaceManager _manager;
    private readonly GalleryDataLoader _loader;

    public GalleryViewModel(SurfaceManager manager, GalleryDataLoader loader)
    {
        _manager = manager;
        _loader = loader;

        // Subscribe to SurfaceManager events
        _manager.SurfaceCreated += OnSurfaceCreated;
        _manager.ComponentsUpdated += OnComponentsUpdated;
        _manager.DataModelUpdated += OnDataModelUpdated;
    }

    // State
    [ObservableProperty] private DemoItem? _selectedItem;
    [ObservableProperty] private int _processedMessageCount;
    [ObservableProperty] private string _currentDataModelJson = "{}";
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<DemoItem> DemoItems { get; } = [];
    public ObservableCollection<string> ActionLogs { get; } = [];

    // Surface — set via SurfaceCreated event, bound to A2UiSurface.Surface in XAML
    public Surface? ActiveSurface { get; private set; }

    // Computed
    public int TotalMessageCount => SelectedItem?.Messages.Length ?? 0;
    public bool CanAdvance => SelectedItem is not null
                              && ProcessedMessageCount < SelectedItem.Messages.Length;

    // Commands
    [RelayCommand(CanExecute = nameof(CanAdvance))]
    private void StepOne() => AdvanceMessages(count: 1);

    [RelayCommand(CanExecute = nameof(CanAdvance))]
    private void StepAll() => AdvanceMessages(all: true);

    [RelayCommand]
    private void Reset() => ResetSurface();

    // Async initialization — called from View.OnOpened
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            var items = await _loader.LoadAsync(ct).ConfigureAwait(true);
            foreach (var item in items) DemoItems.Add(item);
            if (DemoItems.Count > 0) SelectedItem = DemoItems[0];
        }
        finally { IsLoading = false; }
    }

    // View event — code-behind subscribes to call A2UiSurface.Refresh()
    public event EventHandler? SurfaceRefreshRequested;
}
```

**Constructor injection:** `SurfaceManager` and `GalleryDataLoader` are injected
by the DI container — the ViewModel never calls `new` on its dependencies. This
matches the MAUI/WPF convention where ViewModels receive services through their
constructor.

### SurfaceManager event handlers

- `OnSurfaceCreated` → set `ActiveSurface`, raise `PropertyChanged`
- `OnComponentsUpdated` → update `CurrentDataModelJson` from `surface.DataModel.ToJson(indented: true)`, raise `SurfaceRefreshRequested`
- `OnDataModelUpdated` → same: update JSON, raise `SurfaceRefreshRequested`

### Message processing

`AdvanceMessages(bool all = false, int count = 0)`:
- Slice `Messages[ProcessedMessageCount..]` (all) or `Messages[ProcessedMessageCount..+count]`
- Call `_manager.Process(msg)` for each message sequentially
- Increment `ProcessedMessageCount`
- Notify `CanExecuteChanged` on `StepOneCommand` and `StepAllCommand`

### Action logging

`LogAction(UserActionEventArgs e)`:
- Format: `[HH:mm:ss] Action: {e.EventName} on {e.SurfaceId}`
- If `e.Payload` is not null, append `\n{JsonSerializer.Serialize(e.Payload, indentedOptions)}`
- Insert at index 0 of `ActionLogs` (newest first)

### Item selection

`OnSelectedItemChanged(DemoItem? value)`:
- Call `ResetSurface()` then `AdvanceMessages(all: true)`

### Threading

Commands are invoked from UI thread (button clicks). `SurfaceManager.Process()` runs
on UI thread. Events fire on UI thread. No `Dispatcher.UIThread.Post` needed in the
ViewModel for this synchronous gallery app.

### Verification

```bash
cd samples/client/avalonia/gallery_v0_9
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release
```

### Commit
```
feat(avalonia-app): add GalleryViewModel with commands and surface wiring

- InitializeAsync loads DemoItems via GalleryDataLoader
- StepOne, StepAll, Reset commands with CanExecute guards
- SurfaceManager event handlers update inspector state
- Action logging from UserActionFired events
- SurfaceRefreshRequested event for View code-behind
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 6: GalleryWindow Layout + Code-Behind Wiring

**Goal:** Full three-pane window layout matching the lit gallery, with code-behind
wiring for A2UiSurface.Refresh() and UserActionFired.

**Files to modify:**
```
samples/client/avalonia/gallery_v0_9/
├── Views/
│   ├── GalleryWindow.axaml     (rewrite from Step 3 stub)
│   └── GalleryWindow.axaml.cs  (code-behind: Refresh + action wiring)
└── App.axaml                   (add gallery shell styles)
```

### GalleryWindow.axaml Layout

```
DockPanel
  Top: Header bar (title "A2UI Local Gallery", subtitle "v0.9 Catalog")
  Content: Grid (3 columns with GridSplitters)
    Col 0 (250px): Nav sidebar
      ListBox bound to DemoItems, SelectedItem bound to SelectedItem
      ItemTemplate: StackPanel with Title (bold) + Filename (subdued)
    Col 1 (splitter)
    Col 2 (*): Preview pane
      DockPanel:
        Top: Preview header (selected item title, description, stepper controls)
          Stepper: "Messages: N / M" label + Reset / +1 / All buttons
        Fill: ScrollViewer → Border (max-width 600) → A2UiSurface
    Col 3 (splitter)
    Col 4 (340px): Inspector pane
      Grid (2 rows):
        Row 0: Data Model section (header + ScrollViewer → monospace TextBlock)
        Row 1: Action Logs section (header + ListBox with log entries)
```

### App.axaml — Gallery Shell Styles

Application-level dark theme styling for the shell layout only (not renderer components):

| Element | Background | Text | Notes |
|---------|-----------|------|-------|
| Window | `#0f172a` | `#f1f5f9` | Slate-950 |
| Header/Nav panels | `#1e293b` | inherited | Slate-800 |
| Active nav item | `rgba(56,189,248,0.1)` | — | Left border `#38bdf8` |
| Inspector | `#020617` | — | Near-black |
| Stepper buttons | `#38bdf8` fg on `#0f172a` | — | Sky-400 |
| Log entry | `rgba(255,255,255,0.03)` | — | Left border `#38bdf8` |
| Monospace text | Inter/Consolas | `#f1f5f9` | Inspector body |

### GalleryWindow.axaml.cs (Code-Behind)

```csharp
public partial class GalleryWindow : Window
{
    public GalleryWindow()
    {
        InitializeComponent();
        // DataContext is set by App.axaml.cs via DI — not created here
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is GalleryViewModel vm)
        {
            // Wire A2UiSurface.Refresh() to ViewModel event
            vm.SurfaceRefreshRequested += (_, _) =>
                this.FindControl<A2UiSurface>("SurfaceHost")?.Refresh();

            // Wire UserActionFired → ViewModel.LogAction
            var surface = this.FindControl<A2UiSurface>("SurfaceHost");
            if (surface is not null)
                surface.UserActionFired += (_, e) => vm.LogAction(e);
        }
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is GalleryViewModel vm)
        {
            try { await vm.InitializeAsync().ConfigureAwait(true); }
            catch (Exception ex) { /* log to status */ }
        }
    }
}
```

**Note:** `DataContext` is assigned by `App.OnFrameworkInitializationCompleted()`
via DI resolution — the Window never calls `new GalleryViewModel(...)`. This
follows the MAUI pattern where `BindingContext` is set by the shell/DI container.

### Verification

```bash
cd samples/client/avalonia/gallery_v0_9
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release
# If display available:
# DISPLAY=:0 dotnet run
```

### Commit
```
feat(avalonia-app): add GalleryWindow three-pane layout and code-behind

- Three-column Grid: nav sidebar, preview with stepper, inspector
- Dark slate shell styling in App.axaml (application-level, not renderer)
- Code-behind wires A2UiSurface.Refresh() to SurfaceRefreshRequested
- Code-behind wires UserActionFired → GalleryViewModel.LogAction
- Data model JSON inspector + action log panels
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 7: Polish + Documentation

**Goal:** Edge cases, UX refinement, and documentation updates.

### Edge cases
- No JSON files found → show "No examples found" in nav pane
- Malformed JSON file → skip with warning, don't crash
- Empty messages array → show "No messages" in preview
- Surface not initialized → show placeholder text in preview area

### UX refinement
- Auto-select first item on load
- Scroll nav item into view on selection
- Preview: center the surface container, max-width 600px
- Inspector: monospace font for data model JSON
- Stepper buttons disabled state when all messages processed

### Documentation
- Add `README.md` at `samples/client/avalonia/gallery_v0_9/`
- Update `CLAUDE.md` Build & Test section with gallery commands

### Commit
```
feat(avalonia-app): polish gallery UX and add documentation

- Error handling for missing/malformed JSON files
- Auto-select first gallery item on load
- README.md with build and run instructions
- CLAUDE.md updated with gallery build commands
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Summary

| Step | Deliverable | Key Files | Scope |
|------|-------------|-----------|-------|
| 0 | Cleanup | CLAUDE.md, delete old plan | docs |
| 1 | DataModel.ToJson() | DataModel.cs + tests | dotnet-sdk |
| 2 | Renderer default styles | A2UiDefaultStyles.axaml + tests | avalonia-renderer |
| 3 | Gallery scaffold | csproj, Program.cs, App.axaml, GalleryWindow stub | avalonia-app |
| 4 | Data loading | DemoItem.cs, GalleryDataLoader.cs | avalonia-app |
| 5 | ViewModel | GalleryViewModel.cs | avalonia-app |
| 6 | Window layout | GalleryWindow.axaml, code-behind, shell styles | avalonia-app |
| 7 | Polish + docs | Error handling, README.md, CLAUDE.md | avalonia-app + docs |

**Dependency chain:** Steps 0, 1, and 2 are independent (can run in parallel).
Step 3 depends on Step 2 (renderer styles loaded by A2UiSurface).
Steps 4→5→6 are sequential (each builds on previous).
Step 7 depends on Step 6.

**Estimated total:** ~600 LoC across ~10 files, plus the renderer styles AXAML.

---

## Known Limitations (Deferred)

- **`FunctionCall` resolution** — `DynamicValue.FunctionCall` returns null from
  `DataModel.Resolve`. Components using function calls (`formatDate`, `required`,
  etc.) will show blank values.
- **Template children expansion** — `ChildList.Template` renders one item, not N.
  Dynamic lists from data model arrays are not expanded.
- **`createSurface.theme` interpretation** — `primaryColor`, `iconUrl`, and
  `agentDisplayName` are stored but not applied visually.
- **Formal theme injection** — No `ITheme` contract for Avalonia consumer apps
  to programmatically override renderer styles. Current override mechanism is
  Avalonia Style precedence (app styles override renderer defaults).
- **Gradient/glass effects** — The lit gallery's `theme.ts` defines gradient text,
  gradient buttons, and glass-morphism cards. These are CSS-specific techniques
  deferred for a future styled sample.
- **Modal as popup** — `ModalCatalogEntry` renders inline, not as a dialog overlay.
- **Video/Audio playback** — Placeholder text only (no native Avalonia media support).
