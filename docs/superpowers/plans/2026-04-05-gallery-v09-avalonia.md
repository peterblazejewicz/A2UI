# Gallery v0.9 Avalonia Port — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port the A2UI v0.9 local gallery from Lit/TypeScript to a native Avalonia MVVM desktop app that loads 40 spec JSON examples from the filesystem, renders them via `A2UiSurface`, and provides a message stepper + inspector UI.

**Architecture:** Three-pane window (nav sidebar / preview with message stepper / data model + action log inspector). `GalleryDataLoader` reads JSON from `Specs/` directory at runtime. `GalleryViewModel` feeds messages to `SurfaceManager.Process()`. Code-behind wires `A2UiSurface.Refresh()` and `UserActionFired`. DI via `Microsoft.Extensions.DependencyInjection`, MVVM via `CommunityToolkit.Mvvm`.

**Tech Stack:** .NET 10, Avalonia 11.2, CommunityToolkit.Mvvm 8.2, Microsoft.Extensions.DependencyInjection 9.0, xUnit 2.9, FluentAssertions 7.0, Avalonia.Headless.XUnit.

**Spec:** `GALLERY_MIGRATION_PLAN.md` (repo root)

**Environment:**
```bash
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
```

---

## File Map

| File | Responsibility | Task |
|------|---------------|------|
| `COMPOSER_MIGRATION_PLAN.md` | Delete | 1 |
| `CLAUDE.md` | Update repo structure, build commands | 1 |
| `agent_sdks/dotnet/src/A2Ui.Core/DataModel.cs` | Add `ToJson()` method | 2 |
| `agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui/DataModelTests.cs` | Add `ToJson` tests | 2 |
| `renderers/avalonia/src/A2Ui.Avalonia/Themes/A2UiDefaultStyles.axaml` | Default class-backed Styles | 3 |
| `renderers/avalonia/src/A2Ui.Avalonia/Controls/A2UiSurface.cs` | Load default styles | 3 |
| `renderers/avalonia/src/A2Ui.Avalonia/A2Ui.Avalonia.csproj` | Add AvaloniaResource | 3 |
| `renderers/avalonia/tests/A2Ui.Avalonia.Tests/Catalog/DefaultStylesTests.cs` | Style resolution tests | 3 |
| `samples/client/avalonia/gallery_v0_9/A2Ui.Avalonia.Gallery.csproj` | Project file | 4 |
| `samples/client/avalonia/gallery_v0_9/Program.cs` | Entry point + DI | 4 |
| `samples/client/avalonia/gallery_v0_9/App.axaml` | Application + FluentTheme + shell styles | 4 |
| `samples/client/avalonia/gallery_v0_9/App.axaml.cs` | App code-behind + service provider | 4 |
| `samples/client/avalonia/gallery_v0_9/Views/GalleryWindow.axaml` | Stub window | 4 |
| `samples/client/avalonia/gallery_v0_9/Views/GalleryWindow.axaml.cs` | Stub code-behind | 4 |
| `samples/client/avalonia/gallery_v0_9/Models/DemoItem.cs` | Data record | 5 |
| `samples/client/avalonia/gallery_v0_9/Services/GalleryDataLoader.cs` | JSON filesystem loader | 5 |
| `samples/client/avalonia/gallery_v0_9/ViewModels/GalleryViewModel.cs` | MVVM ViewModel | 6 |
| `samples/client/avalonia/gallery_v0_9/Views/GalleryWindow.axaml` | Full three-pane layout | 7 |
| `samples/client/avalonia/gallery_v0_9/Views/GalleryWindow.axaml.cs` | Code-behind wiring | 7 |

---

## Task 1: Preparatory Cleanup

**Files:**
- Delete: `COMPOSER_MIGRATION_PLAN.md`
- Modify: `CLAUDE.md`

- [ ] **Step 1: Delete old plan**

```bash
cd /home/spark/develop/a2-ui
rm COMPOSER_MIGRATION_PLAN.md
```

- [ ] **Step 2: Update CLAUDE.md — Repository Structure**

In `CLAUDE.md`, find the repository structure section and replace the `samples/client/avalonia/` block. Change:

```
│       └── avalonia/                  ← OUR NEW CODE
│           └── composer/              ← port of tools/composer/ to native MVVM
```

To:

```
│       └── avalonia/                  ← OUR NEW CODE
│           ├── gallery_v0_9/          ← v0.9 local gallery (MVVM desktop app)
│           └── composer/              ← placeholder scaffold for future agent-connected app (no source code yet)
```

- [ ] **Step 3: Update CLAUDE.md — Build & Test section**

Find the Avalonia app build commands block and replace:

```bash
# Avalonia app (samples/client/avalonia/composer/)
cd samples/client/avalonia/composer
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release
```

With:

```bash
# Gallery app (samples/client/avalonia/gallery_v0_9/)
cd samples/client/avalonia/gallery_v0_9
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release
```

- [ ] **Step 4: Update CLAUDE.md — Existing Code to Study**

Find `# 5. Composer tool — source for the Avalonia app port` and replace with:

```bash
# 5. Lit gallery — source for the Avalonia gallery port
ls samples/client/lit/gallery_v0_9/
```

- [ ] **Step 5: Update CLAUDE.md — Deliverables**

In the "Project Goal" section, change:

```
- `samples/client/avalonia/` — Composer port: native MVVM desktop app
```

To:

```
- `samples/client/avalonia/` — Gallery v0.9 port + Composer scaffold (future)
```

- [ ] **Step 6: Verify**

```bash
test ! -f COMPOSER_MIGRATION_PLAN.md && echo "PASS: old plan deleted"
grep -q "gallery_v0_9" CLAUDE.md && echo "PASS: CLAUDE.md updated"
```

Expected: Both PASS lines printed.

- [ ] **Step 7: Commit**

```bash
git add CLAUDE.md
git rm COMPOSER_MIGRATION_PLAN.md
git commit -m "$(cat <<'EOF'
chore(docs): replace Composer migration plan with Gallery v0.9

- Delete COMPOSER_MIGRATION_PLAN.md
- Update CLAUDE.md: add gallery_v0_9 to repo structure
- Update build commands to reference gallery app
- Annotate composer/ as placeholder scaffold for future agent app

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: DataModel.ToJson()

**Files:**
- Modify: `agent_sdks/dotnet/src/A2Ui.Core/DataModel.cs`
- Modify: `agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui/DataModelTests.cs`

- [ ] **Step 1: Write failing tests**

Append to `agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui/DataModelTests.cs`, inside the `DataModelTests` class:

```csharp
[Fact]
public void ToJson_EmptyModel_ReturnsEmptyObject()
{
    var dm = new DataModel();
    dm.ToJson().Should().Be("{}");
}

[Fact]
public void ToJson_AfterApply_ReflectsState()
{
    var dm = new DataModel();
    dm.Apply(new UpdateDataModel
    {
        SurfaceId = "s",
        Path = "/name",
        Value = JsonSerializer.SerializeToElement("Alice"),
    });

    var json = dm.ToJson();
    json.Should().Contain("\"name\"");
    json.Should().Contain("\"Alice\"");
}

[Fact]
public void ToJson_Indented_ProducesFormattedOutput()
{
    var dm = new DataModel();
    dm.Apply(new UpdateDataModel
    {
        SurfaceId = "s",
        Path = "/key",
        Value = JsonSerializer.SerializeToElement("val"),
    });

    var json = dm.ToJson(indented: true);
    json.Should().Contain("\n");
    json.Should().Contain("  ");
}
```

- [ ] **Step 2: Run tests — verify they fail**

```bash
cd /home/spark/develop/a2-ui/agent_sdks/dotnet
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test A2Ui.sln --configuration Release --filter "ToJson" --no-restore 2>&1 | tail -5
```

Expected: Build error — `DataModel` does not contain a definition for `ToJson`.

- [ ] **Step 3: Implement ToJson**

In `agent_sdks/dotnet/src/A2Ui.Core/DataModel.cs`, add this method after the `SetSnapshot` method (after line 21):

```csharp
/// <summary>Serialize the current data model state to a JSON string.</summary>
public string ToJson(bool indented = false) =>
    _root.ToJsonString(new JsonSerializerOptions { WriteIndented = indented });
```

- [ ] **Step 4: Run tests — verify they pass**

```bash
cd /home/spark/develop/a2-ui/agent_sdks/dotnet
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test A2Ui.sln --configuration Release --no-restore 2>&1 | tail -3
```

Expected: `Passed!` with all tests passing (including the 3 new ones).

- [ ] **Step 5: Commit**

```bash
cd /home/spark/develop/a2-ui
git add agent_sdks/dotnet/src/A2Ui.Core/DataModel.cs agent_sdks/dotnet/tests/A2Ui.Core.Tests/A2Ui/DataModelTests.cs
git commit -m "$(cat <<'EOF'
feat(dotnet-sdk): add DataModel.ToJson() for state serialization

- Expose _root as JSON string via ToJson(bool indented)
- xUnit tests for empty, populated, and indented output

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: Renderer Default Styles

**Files:**
- Create: `renderers/avalonia/src/A2Ui.Avalonia/Themes/A2UiDefaultStyles.axaml`
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/Controls/A2UiSurface.cs`
- Modify: `renderers/avalonia/src/A2Ui.Avalonia/A2Ui.Avalonia.csproj`
- Create: `renderers/avalonia/tests/A2Ui.Avalonia.Tests/Catalog/DefaultStylesTests.cs`

- [ ] **Step 1: Create the styles AXAML**

Create `renderers/avalonia/src/A2Ui.Avalonia/Themes/A2UiDefaultStyles.axaml`:

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

  <!-- Typography: Heading1-5, Body, Caption -->
  <Style Selector="TextBlock.Heading1">
    <Setter Property="FontSize" Value="32" />
    <Setter Property="FontWeight" Value="Bold" />
    <Setter Property="Margin" Value="0,0,0,4" />
  </Style>
  <Style Selector="TextBlock.Heading2">
    <Setter Property="FontSize" Value="26" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="Margin" Value="0,0,0,4" />
  </Style>
  <Style Selector="TextBlock.Heading3">
    <Setter Property="FontSize" Value="22" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="Margin" Value="0,0,0,2" />
  </Style>
  <Style Selector="TextBlock.Heading4">
    <Setter Property="FontSize" Value="18" />
    <Setter Property="FontWeight" Value="Medium" />
  </Style>
  <Style Selector="TextBlock.Heading5">
    <Setter Property="FontSize" Value="15" />
    <Setter Property="FontWeight" Value="Medium" />
  </Style>
  <Style Selector="TextBlock.Body">
    <Setter Property="FontSize" Value="14" />
  </Style>
  <Style Selector="TextBlock.Caption">
    <Setter Property="FontSize" Value="12" />
    <Setter Property="Opacity" Value="0.7" />
  </Style>

  <!-- Card -->
  <Style Selector="Border.Card">
    <Setter Property="Background" Value="{DynamicResource CardBackgroundFillColorDefaultBrush}" />
    <Setter Property="BorderBrush" Value="{DynamicResource CardStrokeColorDefaultBrush}" />
    <Setter Property="BorderThickness" Value="1" />
    <Setter Property="CornerRadius" Value="8" />
    <Setter Property="Padding" Value="16" />
  </Style>

  <!-- Button danger variant (Fluent provides accent natively) -->
  <Style Selector="Button.danger">
    <Setter Property="Background" Value="#dc2626" />
    <Setter Property="Foreground" Value="White" />
  </Style>
  <Style Selector="Button.danger:pointerover /template/ ContentPresenter#PART_ContentPresenter">
    <Setter Property="Background" Value="#b91c1c" />
  </Style>

</Styles>
```

- [ ] **Step 2: Register AXAML as AvaloniaResource in csproj**

In `renderers/avalonia/src/A2Ui.Avalonia/A2Ui.Avalonia.csproj`, add inside the existing `<Project>` element, after the last `</ItemGroup>`:

```xml
<ItemGroup>
  <AvaloniaResource Include="Themes\A2UiDefaultStyles.axaml" />
</ItemGroup>
```

- [ ] **Step 3: Load styles from A2UiSurface constructor**

In `renderers/avalonia/src/A2Ui.Avalonia/Controls/A2UiSurface.cs`, modify the `A2UiSurface(CatalogRegistry catalog)` constructor. Change:

```csharp
public A2UiSurface(CatalogRegistry catalog)
{
    _renderer = new A2UiRenderer(catalog);
    _renderer.UserActionFired += OnUserActionFired;
}
```

To:

```csharp
public A2UiSurface(CatalogRegistry catalog)
{
    _renderer = new A2UiRenderer(catalog);
    _renderer.UserActionFired += OnUserActionFired;

    // Load default component styles (typography, card, button variants).
    // Consuming apps override these via Application-level styles.
    Styles.Add(new global::Avalonia.Styling.StyleInclude(new Uri("avares://A2Ui.Avalonia"))
    {
        Source = new Uri("avares://A2Ui.Avalonia/Themes/A2UiDefaultStyles.axaml")
    });
}
```

- [ ] **Step 4: Build renderer to verify AXAML compiles**

```bash
cd /home/spark/develop/a2-ui/renderers/avalonia
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build src/A2Ui.Avalonia/A2Ui.Avalonia.csproj --configuration Release 2>&1 | tail -3
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 5: Write style resolution test**

Create `renderers/avalonia/tests/A2Ui.Avalonia.Tests/Catalog/DefaultStylesTests.cs`:

```csharp
using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class DefaultStylesTests
{
    [AvaloniaFact]
    public void TextCatalogEntry_Heading1_HasCorrectClass()
    {
        var entry = new TextCatalogEntry();
        var dm = new DataModel();
        var ctx = new MockRenderContext(dm);
        var component = new A2UiComponent
        {
            Id = "h1",
            Component = "Text",
            Text = DynamicValue.FromString("Title"),
            Variant = "h1",
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<TextBlock>();
        control.Classes.Should().Contain("Heading1");
    }

    [AvaloniaFact]
    public void TextCatalogEntry_Caption_HasCorrectClass()
    {
        var entry = new TextCatalogEntry();
        var dm = new DataModel();
        var ctx = new MockRenderContext(dm);
        var component = new A2UiComponent
        {
            Id = "cap",
            Component = "Text",
            Text = DynamicValue.FromString("Small"),
            Variant = "caption",
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<TextBlock>();
        control.Classes.Should().Contain("Caption");
    }

    [AvaloniaFact]
    public void TextCatalogEntry_DefaultVariant_HasBodyClass()
    {
        var entry = new TextCatalogEntry();
        var dm = new DataModel();
        var ctx = new MockRenderContext(dm);
        var component = new A2UiComponent
        {
            Id = "t1",
            Component = "Text",
            Text = DynamicValue.FromString("Body text"),
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<TextBlock>();
        control.Classes.Should().Contain("Body");
    }

    [AvaloniaFact]
    public void A2UiSurface_Constructor_LoadsDefaultStyles()
    {
        var surface = new A2Ui.Avalonia.Controls.A2UiSurface();

        surface.Styles.Should().NotBeEmpty("A2UiSurface should load A2UiDefaultStyles.axaml");
    }
}
```

- [ ] **Step 6: Run renderer tests**

```bash
cd /home/spark/develop/a2-ui/renderers/avalonia
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/A2Ui.Avalonia.Tests/A2Ui.Avalonia.Tests.csproj --configuration Release 2>&1 | tail -5
```

Expected: `Passed!` with all tests passing.

- [ ] **Step 7: Commit**

```bash
cd /home/spark/develop/a2-ui
git add renderers/avalonia/src/A2Ui.Avalonia/Themes/A2UiDefaultStyles.axaml renderers/avalonia/src/A2Ui.Avalonia/Controls/A2UiSurface.cs renderers/avalonia/src/A2Ui.Avalonia/A2Ui.Avalonia.csproj renderers/avalonia/tests/A2Ui.Avalonia.Tests/Catalog/DefaultStylesTests.cs
git commit -m "$(cat <<'EOF'
fix(avalonia-renderer): add default Styles for component class name hooks

- A2UiDefaultStyles.axaml: Heading1-5, Body, Caption, Card, danger button
- Loaded by A2UiSurface constructor for out-of-box usability
- Fluent-compatible defaults, overridable by consuming app styles
- Tests for class assignment and style loading

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Gallery App Scaffold

**Files:**
- Create: `samples/client/avalonia/gallery_v0_9/A2Ui.Avalonia.Gallery.csproj`
- Create: `samples/client/avalonia/gallery_v0_9/Program.cs`
- Create: `samples/client/avalonia/gallery_v0_9/App.axaml`
- Create: `samples/client/avalonia/gallery_v0_9/App.axaml.cs`
- Create: `samples/client/avalonia/gallery_v0_9/Views/GalleryWindow.axaml`
- Create: `samples/client/avalonia/gallery_v0_9/Views/GalleryWindow.axaml.cs`

- [ ] **Step 1: Create project file**

Create `samples/client/avalonia/gallery_v0_9/A2Ui.Avalonia.Gallery.csproj`:

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
    <Content Include="../../../../specification/v0_9/json/catalogs/minimal/examples/*.json"
             Link="Specs/minimal/%(Filename)%(Extension)"
             CopyToOutputDirectory="PreserveNewest" />
    <Content Include="../../../../specification/v0_9/json/catalogs/basic/examples/*.json"
             Link="Specs/basic/%(Filename)%(Extension)"
             CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create Program.cs**

Create `samples/client/avalonia/gallery_v0_9/Program.cs`.

Note: At this stage `GalleryDataLoader` and `GalleryViewModel` do not exist yet. The DI registration references are added here as commented placeholders. They will be uncommented in Tasks 5 and 6 when those classes are created.

```csharp
using A2Ui.Core;
using Avalonia;
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
        services.AddSingleton<SurfaceManager>();
        // Registered in Task 5: services.AddTransient<GalleryDataLoader>();
        // Registered in Task 6: services.AddSingleton<GalleryViewModel>();
    }
}
```

- [ ] **Step 3: Create App.axaml**

Create `samples/client/avalonia/gallery_v0_9/App.axaml`:

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

- [ ] **Step 4: Create App.axaml.cs**

Create `samples/client/avalonia/gallery_v0_9/App.axaml.cs`.

Note: At scaffold stage, the DataContext assignment uses a simple `new Views.GalleryWindow()` without DI. Task 6 will update this to resolve `GalleryViewModel` from the DI container.

```csharp
using Avalonia;
using Avalonia.Markup.Xaml;

namespace A2Ui.Avalonia.Gallery;

public sealed partial class App : Application
{
    public static IServiceProvider Services { get; set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.GalleryWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

- [ ] **Step 5: Create stub GalleryWindow.axaml**

Create `samples/client/avalonia/gallery_v0_9/Views/GalleryWindow.axaml`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="A2Ui.Avalonia.Gallery.Views.GalleryWindow"
        Title="A2UI Local Gallery — v0.9"
        Width="1200" Height="800"
        Background="#0f172a">
  <TextBlock Text="A2UI Local Gallery — v0.9"
             FontSize="24" FontWeight="Bold"
             Foreground="#f1f5f9"
             HorizontalAlignment="Center"
             VerticalAlignment="Center" />
</Window>
```

- [ ] **Step 6: Create stub GalleryWindow.axaml.cs**

Create `samples/client/avalonia/gallery_v0_9/Views/GalleryWindow.axaml.cs`:

```csharp
using Avalonia.Controls;

namespace A2Ui.Avalonia.Gallery.Views;

public partial class GalleryWindow : Window
{
    public GalleryWindow()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 7: Build to verify scaffold compiles**

```bash
cd /home/spark/develop/a2-ui/samples/client/avalonia/gallery_v0_9
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release 2>&1 | tail -3
```

Expected: `Build succeeded.` with 0 errors. (Warnings about unused `GalleryViewModel` and `GalleryDataLoader` are acceptable at this stage since those classes don't exist yet.)

Note: If build fails because `GalleryViewModel` or `GalleryDataLoader` are not yet defined, temporarily remove those DI registrations from `Program.cs` and the `using` statements. They will be re-added in Tasks 5 and 6. Alternatively, create empty placeholder classes.

- [ ] **Step 8: Commit**

```bash
cd /home/spark/develop/a2-ui
git add samples/client/avalonia/gallery_v0_9/
git commit -m "$(cat <<'EOF'
feat(avalonia-app): scaffold Gallery v0.9 desktop app

- Program.cs with DI container and Avalonia desktop lifetime
- App.axaml with FluentTheme (Dark)
- Stub GalleryWindow proving the app builds
- Spec JSON files linked as Content from specification/

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: DemoItem Model + GalleryDataLoader

**Files:**
- Create: `samples/client/avalonia/gallery_v0_9/Models/DemoItem.cs`
- Create: `samples/client/avalonia/gallery_v0_9/Services/GalleryDataLoader.cs`

- [ ] **Step 1: Create DemoItem.cs**

Create `samples/client/avalonia/gallery_v0_9/Models/DemoItem.cs`:

```csharp
using A2Ui.Core.Messages;

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

- [ ] **Step 2: Create GalleryDataLoader.cs**

Create `samples/client/avalonia/gallery_v0_9/Services/GalleryDataLoader.cs`:

```csharp
using System.Globalization;
using System.Text.Json;
using A2Ui.Avalonia.Gallery.Models;
using A2Ui.Core.Messages;

namespace A2Ui.Avalonia.Gallery.Services;

/// <summary>
/// Loads A2UI spec examples from the Specs/ directory relative to the app executable.
/// Handles both envelope format ({ name, description, messages[] }) and bare array format.
/// Auto-injects a createSurface message if the example doesn't include one.
/// </summary>
public sealed class GalleryDataLoader
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
    };

    public async Task<IReadOnlyList<DemoItem>> LoadAsync(CancellationToken ct = default)
    {
        var items = new List<DemoItem>();
        string specsDir = Path.Combine(AppContext.BaseDirectory, "Specs");

        await LoadFromDirectoryAsync(Path.Combine(specsDir, "minimal"), isBasic: false, items, ct)
            .ConfigureAwait(false);
        await LoadFromDirectoryAsync(Path.Combine(specsDir, "basic"), isBasic: true, items, ct)
            .ConfigureAwait(false);

        return items;
    }

    private async Task LoadFromDirectoryAsync(
        string directory, bool isBasic, List<DemoItem> items, CancellationToken ct)
    {
        if (!Directory.Exists(directory))
            return;

        string[] files = Directory.GetFiles(directory, "*.json");
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        foreach (string filePath in files)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                DemoItem? item = await LoadFileAsync(filePath, isBasic, ct).ConfigureAwait(false);
                if (item is not null)
                    items.Add(item);
            }
            catch (JsonException)
            {
                // Skip malformed files
            }
        }
    }

    private static async Task<DemoItem?> LoadFileAsync(
        string filePath, bool isBasic, CancellationToken ct)
    {
        string json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        // Extract messages — handle both envelope and bare array
        A2UiMessage[] messages;
        string? name = null;
        string? description = null;

        if (root.ValueKind == JsonValueKind.Array)
        {
            messages = JsonSerializer.Deserialize<A2UiMessage[]>(root.GetRawText(), s_jsonOptions)
                       ?? [];
        }
        else
        {
            if (root.TryGetProperty("name", out JsonElement nameEl))
                name = nameEl.GetString();
            if (root.TryGetProperty("description", out JsonElement descEl))
                description = descEl.GetString();

            if (root.TryGetProperty("messages", out JsonElement messagesEl))
                messages = JsonSerializer.Deserialize<A2UiMessage[]>(messagesEl.GetRawText(), s_jsonOptions)
                           ?? [];
            else
                return null;
        }

        if (messages.Length == 0)
            return null;

        string filename = Path.GetFileName(filePath);
        string surfaceId = Path.GetFileNameWithoutExtension(filePath);

        // Auto-inject createSurface if missing
        bool hasCreate = messages.Any(m => m.CreateSurface is not null);
        if (!hasCreate)
        {
            string catalogId = isBasic
                ? "https://a2ui.org/specification/v0_9/basic_catalog.json"
                : "https://a2ui.org/specification/v0_9/catalogs/minimal/minimal_catalog.json";

            var createMsg = new A2UiMessage
            {
                Version = "v0.9",
                CreateSurface = new CreateSurface
                {
                    SurfaceId = surfaceId,
                    CatalogId = catalogId,
                },
            };
            messages = [createMsg, .. messages];
        }
        else
        {
            // Use the surfaceId from the createSurface message
            A2UiMessage? createMessage = messages.FirstOrDefault(m => m.CreateSurface is not null);
            if (createMessage?.CreateSurface is not null)
                surfaceId = createMessage.CreateSurface.SurfaceId;
        }

        string title = name ?? DeriveTitleFromFilename(filename);

        return new DemoItem(
            Id: surfaceId,
            Title: title,
            Filename: filename,
            Description: description ?? $"Source: {filename}",
            Messages: messages,
            IsBasic: isBasic);
    }

    private static string DeriveTitleFromFilename(string filename)
    {
        string stem = Path.GetFileNameWithoutExtension(filename);
        string[] words = stem.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(w =>
            string.IsNullOrEmpty(w) ? w :
            char.ToUpper(w[0], CultureInfo.InvariantCulture) + w[1..]));
    }
}
```

- [ ] **Step 3: Build to verify**

```bash
cd /home/spark/develop/a2-ui/samples/client/avalonia/gallery_v0_9
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release 2>&1 | tail -3
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
cd /home/spark/develop/a2-ui
git add samples/client/avalonia/gallery_v0_9/Models/DemoItem.cs samples/client/avalonia/gallery_v0_9/Services/GalleryDataLoader.cs
git commit -m "$(cat <<'EOF'
feat(avalonia-app): add DemoItem model and GalleryDataLoader

- DemoItem sealed record with Id, Title, Filename, Description, Messages
- GalleryDataLoader: reads Specs/ directory at runtime
- Handles envelope and bare array JSON formats
- Auto-injects createSurface when missing (matches lit gallery)
- Title derived from filename

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: GalleryViewModel

**Files:**
- Create: `samples/client/avalonia/gallery_v0_9/ViewModels/GalleryViewModel.cs`

- [ ] **Step 1: Create GalleryViewModel.cs**

Create `samples/client/avalonia/gallery_v0_9/ViewModels/GalleryViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using System.Text.Json;
using A2Ui.Avalonia.Gallery.Models;
using A2Ui.Avalonia.Gallery.Services;
using A2Ui.Core;
using A2Ui.Core.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace A2Ui.Avalonia.Gallery.ViewModels;

public sealed partial class GalleryViewModel : ObservableObject, IDisposable
{
    private static readonly JsonSerializerOptions s_indentedJson = new() { WriteIndented = true };

    private readonly SurfaceManager _manager;
    private readonly GalleryDataLoader _loader;

    public GalleryViewModel(SurfaceManager manager, GalleryDataLoader loader)
    {
        _manager = manager;
        _loader = loader;

        _manager.SurfaceCreated += OnSurfaceCreated;
        _manager.SurfaceDeleted += OnSurfaceDeleted;
        _manager.ComponentsUpdated += OnComponentsUpdated;
        _manager.DataModelUpdated += OnDataModelUpdated;
    }

    // ── State ──────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalMessageCount))]
    [NotifyPropertyChangedFor(nameof(CanAdvance))]
    private DemoItem? _selectedItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAdvance))]
    [NotifyCanExecuteChangedFor(nameof(StepOneCommand))]
    [NotifyCanExecuteChangedFor(nameof(StepAllCommand))]
    private int _processedMessageCount;

    [ObservableProperty]
    private string _currentDataModelJson = "{}";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private Surface? _activeSurface;

    public ObservableCollection<DemoItem> DemoItems { get; } = [];
    public ObservableCollection<string> ActionLogs { get; } = [];

    // ── Computed ───────────────────────────────────────────

    public int TotalMessageCount => SelectedItem?.Messages.Length ?? 0;
    public bool CanAdvance => SelectedItem is not null
                              && ProcessedMessageCount < SelectedItem.Messages.Length;

    // ── Commands ──────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanAdvance))]
    private void StepOne() => AdvanceMessages(count: 1);

    [RelayCommand(CanExecute = nameof(CanAdvance))]
    private void StepAll() => AdvanceMessages(all: true);

    [RelayCommand]
    private void Reset() => ResetSurface();

    // ── View event ────────────────────────────────────────

    /// <summary>
    /// Raised when the code-behind should call A2UiSurface.Refresh().
    /// The ViewModel cannot touch UI controls directly.
    /// </summary>
    public event EventHandler? SurfaceRefreshRequested;

    // ── Initialization ────────────────────────────────────

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            IReadOnlyList<DemoItem> items = await _loader.LoadAsync(ct).ConfigureAwait(true);
            foreach (DemoItem item in items)
                DemoItems.Add(item);

            if (DemoItems.Count > 0)
                SelectedItem = DemoItems[0];
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Item selection ────────────────────────────────────

    partial void OnSelectedItemChanged(DemoItem? value)
    {
        if (value is null) return;
        ResetSurface();
        AdvanceMessages(all: true);
    }

    // ── Message processing ────────────────────────────────

    private void AdvanceMessages(bool all = false, int count = 0)
    {
        DemoItem? item = SelectedItem;
        if (item is null) return;

        int start = ProcessedMessageCount;
        int end = all ? item.Messages.Length : Math.Min(start + count, item.Messages.Length);

        for (int i = start; i < end; i++)
            _manager.Process(item.Messages[i]);

        ProcessedMessageCount = end;
    }

    private void ResetSurface()
    {
        DemoItem? item = SelectedItem;
        if (item is null) return;

        // Delete existing surface if present
        if (_manager.GetSurface(item.Id) is not null)
        {
            _manager.Process(new A2UiMessage
            {
                Version = "v0.9",
                DeleteSurface = new DeleteSurface { SurfaceId = item.Id },
            });
        }

        ProcessedMessageCount = 0;
        CurrentDataModelJson = "{}";
        ActionLogs.Clear();
        ActiveSurface = null;
    }

    // ── Action logging ────────────────────────────────────

    public void LogAction(UserActionEventArgs e)
    {
        string time = DateTime.Now.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        string entry = $"[{time}] Action: {e.EventName} on {e.SurfaceId}";

        if (e.Payload is not null)
        {
            try
            {
                string payload = JsonSerializer.Serialize(e.Payload, s_indentedJson);
                entry += $"\n{payload}";
            }
            catch (JsonException)
            {
                entry += $"\n{e.Payload}";
            }
        }

        ActionLogs.Insert(0, entry);
    }

    // ── SurfaceManager event handlers ─────────────────────

    private void OnSurfaceCreated(object? sender, SurfaceCreatedEventArgs e)
    {
        ActiveSurface = e.Surface;
        UpdateDataModelJson(e.Surface);
    }

    private void OnSurfaceDeleted(object? sender, SurfaceDeletedEventArgs e)
    {
        if (ActiveSurface?.SurfaceId == e.Surface.SurfaceId)
            ActiveSurface = null;
    }

    private void OnComponentsUpdated(object? sender, ComponentsUpdatedEventArgs e)
    {
        UpdateDataModelJson(e.Surface);
        SurfaceRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnDataModelUpdated(object? sender, DataModelUpdatedEventArgs e)
    {
        UpdateDataModelJson(e.Surface);
        SurfaceRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateDataModelJson(Surface surface) =>
        CurrentDataModelJson = surface.DataModel.ToJson(indented: true);

    // ── Dispose ───────────────────────────────────────────

    public void Dispose()
    {
        _manager.SurfaceCreated -= OnSurfaceCreated;
        _manager.SurfaceDeleted -= OnSurfaceDeleted;
        _manager.ComponentsUpdated -= OnComponentsUpdated;
        _manager.DataModelUpdated -= OnDataModelUpdated;
    }
}
```

- [ ] **Step 2: Wire DI registrations in Program.cs**

In `samples/client/avalonia/gallery_v0_9/Program.cs`, uncomment and update the DI registrations. Add the using statements and replace `ConfigureServices`:

```csharp
using A2Ui.Avalonia.Gallery.Services;
using A2Ui.Avalonia.Gallery.ViewModels;
using A2Ui.Core;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
```

```csharp
private static void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<SurfaceManager>();
    services.AddTransient<GalleryDataLoader>();
    services.AddSingleton<GalleryViewModel>();
}
```

- [ ] **Step 3: Wire DI in App.axaml.cs**

Replace `samples/client/avalonia/gallery_v0_9/App.axaml.cs` with:

```csharp
using A2Ui.Avalonia.Gallery.ViewModels;
using Avalonia;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace A2Ui.Avalonia.Gallery;

public sealed partial class App : Application
{
    public static IServiceProvider Services { get; set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
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

- [ ] **Step 4: Build to verify**

```bash
cd /home/spark/develop/a2-ui/samples/client/avalonia/gallery_v0_9
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release 2>&1 | tail -3
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
cd /home/spark/develop/a2-ui
git add samples/client/avalonia/gallery_v0_9/ViewModels/GalleryViewModel.cs samples/client/avalonia/gallery_v0_9/Program.cs samples/client/avalonia/gallery_v0_9/App.axaml.cs
git commit -m "$(cat <<'EOF'
feat(avalonia-app): add GalleryViewModel with commands and surface wiring

- Constructor injection of SurfaceManager and GalleryDataLoader
- InitializeAsync loads DemoItems from filesystem
- StepOne, StepAll, Reset commands with CanExecute guards
- SurfaceManager event handlers update inspector state
- Action logging from UserActionFired events
- SurfaceRefreshRequested event for View code-behind
- IDisposable for event handler cleanup

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: GalleryWindow Three-Pane Layout + Code-Behind

**Files:**
- Modify: `samples/client/avalonia/gallery_v0_9/Views/GalleryWindow.axaml`
- Modify: `samples/client/avalonia/gallery_v0_9/Views/GalleryWindow.axaml.cs`
- Modify: `samples/client/avalonia/gallery_v0_9/App.axaml`

- [ ] **Step 1: Add shell styles to App.axaml**

Replace `samples/client/avalonia/gallery_v0_9/App.axaml` with:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="A2Ui.Avalonia.Gallery.App"
             RequestedThemeVariant="Dark">
  <Application.Styles>
    <FluentTheme />

    <!-- Gallery shell styles (application-level, not renderer) -->
    <Style Selector="ListBoxItem:selected /template/ ContentPresenter">
      <Setter Property="Background" Value="#0E3856" />
    </Style>
  </Application.Styles>

  <Application.Resources>
    <SolidColorBrush x:Key="GalleryBackground" Color="#0f172a" />
    <SolidColorBrush x:Key="GalleryPanel" Color="#1e293b" />
    <SolidColorBrush x:Key="GalleryInspector" Color="#020617" />
    <SolidColorBrush x:Key="GalleryAccent" Color="#38bdf8" />
    <SolidColorBrush x:Key="GalleryText" Color="#f1f5f9" />
    <SolidColorBrush x:Key="GallerySubtext" Color="#94a3b8" />
    <SolidColorBrush x:Key="GallerySurface" Color="#0B1625" />
    <SolidColorBrush x:Key="GalleryBorder" Color="#1e3a5f" />
  </Application.Resources>
</Application>
```

- [ ] **Step 2: Write full GalleryWindow.axaml**

Replace `samples/client/avalonia/gallery_v0_9/Views/GalleryWindow.axaml` with:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:A2Ui.Avalonia.Gallery.ViewModels"
        xmlns:models="using:A2Ui.Avalonia.Gallery.Models"
        xmlns:controls="using:A2Ui.Avalonia.Controls"
        x:Class="A2Ui.Avalonia.Gallery.Views.GalleryWindow"
        x:DataType="vm:GalleryViewModel"
        Title="A2UI Local Gallery — v0.9"
        Width="1280" Height="800"
        Background="{DynamicResource GalleryBackground}"
        Foreground="{DynamicResource GalleryText}">

  <DockPanel>
    <!-- Header -->
    <Border DockPanel.Dock="Top"
            Background="{DynamicResource GalleryPanel}"
            BorderBrush="{DynamicResource GalleryBorder}"
            BorderThickness="0,0,0,1"
            Padding="24,12">
      <StackPanel>
        <TextBlock Text="A2UI Local Gallery"
                   FontSize="20" FontWeight="Bold" />
        <TextBlock Text="v0.9 Catalog"
                   FontSize="13"
                   Foreground="{DynamicResource GallerySubtext}" />
      </StackPanel>
    </Border>

    <!-- Three-pane grid -->
    <Grid>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="250" MinWidth="180" />
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition Width="340" MinWidth="200" />
      </Grid.ColumnDefinitions>

      <!-- Nav sidebar -->
      <Border Grid.Column="0"
              Background="{DynamicResource GalleryPanel}"
              BorderBrush="{DynamicResource GalleryBorder}"
              BorderThickness="0,0,1,0">
        <ListBox ItemsSource="{Binding DemoItems}"
                 SelectedItem="{Binding SelectedItem}"
                 Background="Transparent"
                 BorderThickness="0">
          <ListBox.ItemTemplate>
            <DataTemplate x:DataType="models:DemoItem">
              <StackPanel Margin="4,8">
                <TextBlock Text="{Binding Title}"
                           FontWeight="Medium"
                           FontSize="13" />
                <TextBlock Text="{Binding Filename}"
                           FontSize="11"
                           Foreground="{DynamicResource GallerySubtext}" />
              </StackPanel>
            </DataTemplate>
          </ListBox.ItemTemplate>
        </ListBox>
      </Border>

      <GridSplitter Grid.Column="1" Width="4"
                    Background="Transparent" />

      <!-- Preview pane -->
      <DockPanel Grid.Column="2">
        <!-- Preview header + stepper -->
        <Border DockPanel.Dock="Top"
                Background="{DynamicResource GalleryPanel}"
                BorderBrush="{DynamicResource GalleryBorder}"
                BorderThickness="0,0,0,1"
                Padding="16,10">
          <Grid>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="*" />
              <ColumnDefinition Width="Auto" />
            </Grid.ColumnDefinitions>

            <StackPanel Grid.Column="0" VerticalAlignment="Center">
              <TextBlock Text="{Binding SelectedItem.Title, FallbackValue='Select an example'}"
                         FontSize="16" FontWeight="SemiBold" />
              <TextBlock Text="{Binding SelectedItem.Description}"
                         FontSize="12"
                         Foreground="{DynamicResource GallerySubtext}"
                         Margin="0,2,0,0" />
            </StackPanel>

            <StackPanel Grid.Column="1"
                        Orientation="Horizontal"
                        Spacing="8"
                        VerticalAlignment="Center">
              <TextBlock VerticalAlignment="Center"
                         Foreground="{DynamicResource GallerySubtext}"
                         FontSize="13">
                <Run Text="Messages: " />
                <Run Text="{Binding ProcessedMessageCount}" />
                <Run Text=" / " />
                <Run Text="{Binding TotalMessageCount}" />
              </TextBlock>
              <Button Content="Reset"
                      Command="{Binding ResetCommand}"
                      Padding="8,4" FontSize="12" />
              <Button Content="+1 Message"
                      Command="{Binding StepOneCommand}"
                      Padding="8,4" FontSize="12" />
              <Button Content="All Messages"
                      Command="{Binding StepAllCommand}"
                      Padding="8,4" FontSize="12" />
            </StackPanel>
          </Grid>
        </Border>

        <!-- Surface area -->
        <ScrollViewer HorizontalScrollBarVisibility="Disabled"
                      VerticalScrollBarVisibility="Auto"
                      Padding="24">
          <Border MaxWidth="600"
                  HorizontalAlignment="Center"
                  Background="{DynamicResource GallerySurface}"
                  BorderBrush="{DynamicResource GalleryBorder}"
                  BorderThickness="1"
                  CornerRadius="8"
                  Padding="24">
            <controls:A2UiSurface x:Name="SurfaceHost"
                                  Surface="{Binding ActiveSurface}" />
          </Border>
        </ScrollViewer>
      </DockPanel>

      <GridSplitter Grid.Column="3" Width="4"
                    Background="Transparent" />

      <!-- Inspector pane -->
      <Border Grid.Column="4"
              Background="{DynamicResource GalleryInspector}"
              BorderBrush="{DynamicResource GalleryBorder}"
              BorderThickness="1,0,0,0">
        <Grid>
          <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
          </Grid.RowDefinitions>

          <!-- Data Model -->
          <DockPanel Grid.Row="0">
            <Border DockPanel.Dock="Top"
                    Background="{DynamicResource GalleryPanel}"
                    Padding="16,8">
              <TextBlock Text="DATA MODEL"
                         FontSize="11" FontWeight="Bold"
                         Foreground="{DynamicResource GallerySubtext}" />
            </Border>
            <ScrollViewer Padding="16">
              <TextBlock Text="{Binding CurrentDataModelJson}"
                         FontFamily="Consolas,Menlo,monospace"
                         FontSize="11"
                         TextWrapping="Wrap"
                         Foreground="{DynamicResource GalleryText}" />
            </ScrollViewer>
          </DockPanel>

          <GridSplitter Grid.Row="1" Height="4"
                        Background="Transparent"
                        ResizeDirection="Rows" />

          <!-- Action Logs -->
          <DockPanel Grid.Row="2">
            <Border DockPanel.Dock="Top"
                    Background="{DynamicResource GalleryPanel}"
                    Padding="16,8">
              <TextBlock Text="ACTION LOGS"
                         FontSize="11" FontWeight="Bold"
                         Foreground="{DynamicResource GallerySubtext}" />
            </Border>
            <ListBox ItemsSource="{Binding ActionLogs}"
                     Background="Transparent"
                     BorderThickness="0"
                     Padding="8">
              <ListBox.ItemTemplate>
                <DataTemplate>
                  <Border BorderBrush="{DynamicResource GalleryAccent}"
                          BorderThickness="2,0,0,0"
                          Padding="8,4"
                          Margin="0,2">
                    <TextBlock Text="{Binding}"
                               FontFamily="Consolas,Menlo,monospace"
                               FontSize="11"
                               TextWrapping="Wrap"
                               Foreground="{DynamicResource GalleryText}" />
                  </Border>
                </DataTemplate>
              </ListBox.ItemTemplate>
            </ListBox>
          </DockPanel>
        </Grid>
      </Border>
    </Grid>
  </DockPanel>
</Window>
```

- [ ] **Step 3: Write code-behind with wiring**

Replace `samples/client/avalonia/gallery_v0_9/Views/GalleryWindow.axaml.cs` with:

```csharp
using A2Ui.Avalonia.Controls;
using A2Ui.Avalonia.Gallery.ViewModels;
using Avalonia.Controls;

namespace A2Ui.Avalonia.Gallery.Views;

public partial class GalleryWindow : Window
{
    public GalleryWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is GalleryViewModel vm)
        {
            // Wire A2UiSurface.Refresh() — ViewModel cannot touch UI controls
            vm.SurfaceRefreshRequested += (_, _) =>
                this.FindControl<A2UiSurface>("SurfaceHost")?.Refresh();

            // Wire UserActionFired → ViewModel action logging
            A2UiSurface? surface = this.FindControl<A2UiSurface>("SurfaceHost");
            if (surface is not null)
                surface.UserActionFired += (_, args) => vm.LogAction(args);
        }
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is GalleryViewModel vm)
        {
            try
            {
                await vm.InitializeAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                // Show error in title bar if loading fails
                Title = $"A2UI Gallery — Error: {ex.Message}";
            }
        }
    }
}
```

- [ ] **Step 4: Build full app**

```bash
cd /home/spark/develop/a2-ui/samples/client/avalonia/gallery_v0_9
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release 2>&1 | tail -5
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 5: Commit**

```bash
cd /home/spark/develop/a2-ui
git add samples/client/avalonia/gallery_v0_9/Views/ samples/client/avalonia/gallery_v0_9/App.axaml
git commit -m "$(cat <<'EOF'
feat(avalonia-app): add GalleryWindow three-pane layout and code-behind

- Three-column Grid: nav sidebar, preview with stepper, inspector
- Dark slate shell styling via Application.Resources
- Code-behind wires A2UiSurface.Refresh() to SurfaceRefreshRequested
- Code-behind wires UserActionFired to GalleryViewModel.LogAction
- Data model JSON inspector + action log panels
- GridSplitters for resizable panes

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 8: Polish + Documentation

**Files:**
- Create: `samples/client/avalonia/gallery_v0_9/README.md`

- [ ] **Step 1: Create README.md**

Create `samples/client/avalonia/gallery_v0_9/README.md`:

```markdown
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
```

- [ ] **Step 2: Run full verification**

```bash
cd /home/spark/develop/a2-ui

# SDK tests still pass
cd agent_sdks/dotnet
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test A2Ui.sln --configuration Release 2>&1 | tail -3

# Renderer tests still pass
cd ../../renderers/avalonia
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/A2Ui.Avalonia.Tests/A2Ui.Avalonia.Tests.csproj --configuration Release 2>&1 | tail -3

# Gallery builds
cd ../../samples/client/avalonia/gallery_v0_9
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release 2>&1 | tail -3
```

Expected: All three pass with `Build succeeded` / `Passed!`.

- [ ] **Step 3: Commit**

```bash
cd /home/spark/develop/a2-ui
git add samples/client/avalonia/gallery_v0_9/README.md
git commit -m "$(cat <<'EOF'
docs(avalonia-app): add Gallery v0.9 README

- Build and run instructions
- Architecture overview
- Guide for adding custom examples

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Final Checklist

After all tasks complete:

- [ ] `COMPOSER_MIGRATION_PLAN.md` deleted
- [ ] `CLAUDE.md` references `gallery_v0_9/`
- [ ] `DataModel.ToJson()` exists and is tested
- [ ] `A2UiDefaultStyles.axaml` loaded by `A2UiSurface`
- [ ] Gallery app builds: `dotnet build --configuration Release`
- [ ] SDK tests pass: `dotnet test A2Ui.sln --configuration Release`
- [ ] Renderer tests pass: `dotnet test A2Ui.Avalonia.Tests.csproj --configuration Release`
- [ ] All 8 commits present on `feature/dotnet-avalonia-renderer`
