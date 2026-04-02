---
name: a2ui_avalonia_renderer
description: Build Avalonia A2UI renderer: CatalogRegistry, 13 ICatalogEntry implementations, A2UiRenderer, A2UiSurface UserControl, AgentEventBridge.
---


---

## Purpose

Build the `A2Ui.Avalonia` class library — the Avalonia renderer that maps A2UI
abstract component types to native Avalonia controls. This is the first .NET/C#
A2UI renderer; existing renderers target Angular, Flutter, Lit, and Markdown.

Target: ~3000 LoC across catalog, factory, renderer, and bindings.
All Avalonia packages are MIT licensed.

---

## Architecture Overview

```
SurfaceManager (A2Ui.Core)
    ↓ ComponentsUpdated event
A2UiRenderer (A2Ui.Avalonia)
    ↓ for each component
CatalogRegistry → IComponentFactory
    ↓ creates
Avalonia Control
    ↓ BoundValue paths resolved via
DataModel (A2Ui.Core)
    ↓ wired to
Avalonia reactive properties
```

---

## Phase 1 — Project Setup

Edit `src/A2Ui.Avalonia/A2Ui.Avalonia.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>A2Ui.Avalonia</AssemblyName>
    <RootNamespace>A2Ui.Avalonia</RootNamespace>
    <Description>A2UI renderer for Avalonia UI</Description>
    <!-- Required for Avalonia control library -->
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" />
    <PackageReference Include="Avalonia.Themes.Fluent" />
    <PackageReference Include="CommunityToolkit.Mvvm" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../A2Ui.Core/A2Ui.Core.csproj" />
    <ProjectReference Include="../AgUi.Protocol/AgUi.Protocol.csproj" />
  </ItemGroup>
</Project>
```

---

## Phase 2 — Catalog Registry

Create `src/A2Ui.Avalonia/Catalog/ICatalogEntry.cs`:

```csharp
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;

namespace A2Ui.Avalonia.Catalog;

/// <summary>
/// Factory contract for a single A2UI component type.
/// Implement this for each type you add to the catalog.
/// </summary>
public interface ICatalogEntry
{
    /// <summary>
    /// A2UI component type string, e.g. "Text", "Button", "Column".
    /// Must exactly match the value in A2UiComponent.Component.
    /// </summary>
    string ComponentType { get; }

    /// <summary>
    /// Create an Avalonia control for this component.
    /// Called on the UI thread (Dispatcher).
    /// </summary>
    Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context);

    /// <summary>
    /// Update an existing control in-place (avoids full recreate).
    /// Return false to signal the renderer should recreate instead.
    /// </summary>
    bool Update(Control existing, A2UiComponent component, DataModel dataModel,
                IRenderContext context);
}

/// <summary>Context passed to factory methods for cross-cutting concerns.</summary>
public interface IRenderContext
{
    /// <summary>Render a child component by ID.</summary>
    Control? RenderChild(string? childId);

    /// <summary>Render all children of a component.</summary>
    IEnumerable<Control> RenderChildren(string parentId);

    /// <summary>Fire a user action event back to the agent.</summary>
    void FireUserAction(string surfaceId, string eventName, object? payload = null);

    /// <summary>Resolve a BoundOrLiteral value from the data model.</summary>
    string? Resolve(BoundOrLiteral? bound);
}
```

Create `src/A2Ui.Avalonia/Catalog/CatalogRegistry.cs`:

```csharp
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;

namespace A2Ui.Avalonia.Catalog;

/// <summary>
/// Registry of A2UI type → Avalonia control factory mappings.
/// The agent may ONLY reference types registered here — security boundary.
/// </summary>
public sealed class CatalogRegistry
{
    private readonly Dictionary<string, ICatalogEntry> _entries = new();

    /// <summary>Register a catalog entry. Throws if already registered.</summary>
    public CatalogRegistry Register(ICatalogEntry entry)
    {
        _entries.Add(entry.ComponentType, entry);
        return this;
    }

    /// <summary>Register a simple factory function without implementing ICatalogEntry.</summary>
    public CatalogRegistry Register(string componentType,
        Func<A2UiComponent, DataModel, IRenderContext, Control> factory)
    {
        return Register(new DelegateCatalogEntry(componentType, factory));
    }

    public bool TryGetEntry(string componentType, out ICatalogEntry? entry) =>
        _entries.TryGetValue(componentType, out entry);

    public IReadOnlyCollection<string> RegisteredTypes => _entries.Keys;

    /// <summary>Build the default catalog with all built-in component types.</summary>
    public static CatalogRegistry CreateDefault() => new CatalogRegistry()
        .Register(new TextCatalogEntry())
        .Register(new ButtonCatalogEntry())
        .Register(new ColumnCatalogEntry())
        .Register(new RowCatalogEntry())
        .Register(new TextFieldCatalogEntry())
        .Register(new DateTimeInputCatalogEntry())
        .Register(new CardCatalogEntry())
        .Register(new ImageCatalogEntry())
        .Register(new SelectCatalogEntry())
        .Register(new CheckboxCatalogEntry())
        .Register(new SliderCatalogEntry())
        .Register(new TableCatalogEntry())
        .Register(new SurfaceCatalogEntry());
}

internal sealed class DelegateCatalogEntry(
    string componentType,
    Func<A2UiComponent, DataModel, IRenderContext, Control> factory) : ICatalogEntry
{
    public string ComponentType => componentType;

    public Control Create(A2UiComponent component, DataModel dataModel,
                          IRenderContext context) =>
        factory(component, dataModel, context);

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel,
                       IRenderContext context) => false; // recreate by default
}
```

---

## Phase 3 — Core Catalog Entries

Create `src/A2Ui.Avalonia/Catalog/Entries/TextCatalogEntry.cs`:

```csharp
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Layout;

namespace A2Ui.Avalonia.Catalog;

/// <summary>
/// A2UI "Text" → Avalonia TextBlock.
/// Variants: h1, h2, h3, body (default), caption, label.
/// </summary>
public sealed class TextCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Text";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var tb = new TextBlock
        {
            Text = ctx.Resolve(c.Text) ?? string.Empty,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };

        ApplyVariant(tb, c.Variant);
        return tb;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        if (existing is not TextBlock tb) return false;
        tb.Text = ctx.Resolve(c.Text) ?? string.Empty;
        ApplyVariant(tb, c.Variant);
        return true;
    }

    private static void ApplyVariant(TextBlock tb, string? variant)
    {
        tb.Classes.Clear();
        tb.Classes.Add(variant switch
        {
            "h1"      => "Heading1",
            "h2"      => "Heading2",
            "h3"      => "Heading3",
            "caption" => "Caption",
            "label"   => "Label",
            _         => "Body",
        });
    }
}
```

Create `src/A2Ui.Avalonia/Catalog/Entries/ButtonCatalogEntry.cs`:

```csharp
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;

namespace A2Ui.Avalonia.Catalog;

public sealed class ButtonCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Button";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var btn = new Button
        {
            Content = c.Child is not null
                ? ctx.RenderChild(c.Child)
                : (object?)(ctx.Resolve(c.Text) ?? c.Label ?? string.Empty),
        };

        ApplyVariant(btn, c.Variant);

        if (c.Action?.Event is { } actionEvent)
        {
            string surfaceId = c.Parent ?? string.Empty; // resolved by renderer context
            string eventName = actionEvent.Name;
            btn.Click += (_, _) => ctx.FireUserAction(surfaceId, eventName);
        }

        return btn;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false; // recreate for simplicity

    private static void ApplyVariant(Button btn, string? variant)
    {
        btn.Classes.Clear();
        if (variant is "primary") btn.Classes.Add("accent");
        else if (variant is "danger") btn.Classes.Add("danger");
    }
}
```

Create `src/A2Ui.Avalonia/Catalog/Entries/LayoutCatalogEntries.cs`:

```csharp
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Layout;

namespace A2Ui.Avalonia.Catalog;

/// <summary>A2UI "Column" → StackPanel (Vertical).</summary>
public sealed class ColumnCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Column";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        foreach (var child in ctx.RenderChildren(c.Id))
            panel.Children.Add(child);
        return panel;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "Row" → StackPanel (Horizontal) with wrap.</summary>
public sealed class RowCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Row";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var panel = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var child in ctx.RenderChildren(c.Id))
            panel.Children.Add(child);
        return panel;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "Card" → Border with shadow effect.</summary>
public sealed class CardCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Card";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var border = new Border
        {
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(16),
            Classes = { "Card" },
        };
        var panel = new StackPanel { Spacing = 8 };
        foreach (var child in ctx.RenderChildren(c.Id))
            panel.Children.Add(child);
        border.Child = panel;
        return border;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}
```

Create `src/A2Ui.Avalonia/Catalog/Entries/InputCatalogEntries.cs`:

```csharp
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;

namespace A2Ui.Avalonia.Catalog;

/// <summary>A2UI "TextField" → Avalonia TextBox.</summary>
public sealed class TextFieldCatalogEntry : ICatalogEntry
{
    public string ComponentType => "TextField";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var tb = new TextBox
        {
            Text        = ctx.Resolve(c.Value) ?? string.Empty,
            Watermark   = c.Label,
        };
        return tb;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx)
    {
        if (existing is not TextBox tb) return false;
        if (!tb.IsFocused) tb.Text = ctx.Resolve(c.Value) ?? string.Empty;
        tb.Watermark = c.Label;
        return true;
    }
}

/// <summary>A2UI "DateTimeInput" → CalendarDatePicker or TimePicker.</summary>
public sealed class DateTimeInputCatalogEntry : ICatalogEntry
{
    public string ComponentType => "DateTimeInput";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        // Simple implementation — full implementation uses a combined picker
        var picker = new CalendarDatePicker
        {
            Watermark = c.Label ?? "Select date",
        };

        var raw = ctx.Resolve(c.Value);
        if (raw is not null && DateOnly.TryParse(raw, out var date))
            picker.SelectedDate = date.ToDateTime(TimeOnly.MinValue);

        return picker;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "Select" → ComboBox.</summary>
public sealed class SelectCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Select";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var combo = new ComboBox
        {
            PlaceholderText = c.Label,
        };
        return combo;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "Checkbox" → CheckBox.</summary>
public sealed class CheckboxCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Checkbox";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        bool isChecked = ctx.Resolve(c.Value) is "true";
        return new CheckBox
        {
            Content     = c.Label,
            IsChecked   = isChecked,
        };
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx)
    {
        if (existing is not CheckBox cb) return false;
        if (!cb.IsFocused) cb.IsChecked = ctx.Resolve(c.Value) is "true";
        cb.Content = c.Label;
        return true;
    }
}

/// <summary>A2UI "Slider" → Avalonia Slider.</summary>
public sealed class SliderCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Slider";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        double.TryParse(ctx.Resolve(c.Value), out double val);
        return new Slider { Value = val, Minimum = 0, Maximum = 100 };
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx)
    {
        if (existing is not Slider s) return false;
        if (!s.IsFocused && double.TryParse(ctx.Resolve(c.Value), out double val))
            s.Value = val;
        return true;
    }
}
```

Create `src/A2Ui.Avalonia/Catalog/Entries/MediaCatalogEntries.cs`:

```csharp
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace A2Ui.Avalonia.Catalog;

/// <summary>A2UI "Image" → Avalonia Image. Loads from URL asynchronously.</summary>
public sealed class ImageCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Image";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var img = new Image { Stretch = Avalonia.Media.Stretch.Uniform };
        string? url = c.Url ?? ctx.Resolve(c.Value);
        if (url is not null) LoadAsync(img, url);
        return img;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;

    private static async void LoadAsync(Image img, string url)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient();
            await using var stream = await http.GetStreamAsync(url).ConfigureAwait(true);
            img.Source = new Bitmap(stream);
        }
        catch { /* silently fail — broken image stays empty */ }
    }
}

/// <summary>A2UI "Table" → Avalonia DataGrid.</summary>
public sealed class TableCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Table";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var grid = new DataGrid { CanUserReorderColumns = true, IsReadOnly = true };
        if (c.Columns is { } cols)
        {
            foreach (var col in cols)
            {
                grid.Columns.Add(new DataGridTextColumn
                {
                    Header  = col.Header,
                    Binding = new Avalonia.Data.Binding(col.Field),
                });
            }
        }
        return grid;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "Surface" → UserControl root container.</summary>
public sealed class SurfaceCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Surface";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var panel = new StackPanel { Spacing = 12 };
        foreach (var child in ctx.RenderChildren(c.Id))
            panel.Children.Add(child);
        return panel;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}
```

---

## Phase 4 — A2UI Renderer Engine

Create `src/A2Ui.Avalonia/A2UiRenderer.cs`:

```csharp
using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Threading;

namespace A2Ui.Avalonia;

/// <summary>
/// Renders an A2UI surface to Avalonia controls.
/// Must be called on the UI thread (Avalonia Dispatcher).
/// </summary>
public sealed class A2UiRenderer
{
    private readonly CatalogRegistry _catalog;
    private readonly Dictionary<string, Control> _controlCache = new();

    public A2UiRenderer(CatalogRegistry catalog)
    {
        _catalog = catalog;
    }

    /// <summary>
    /// Render or update the complete surface.
    /// Returns the root control for insertion into the visual tree.
    /// </summary>
    public Control Render(Surface surface)
    {
        Dispatcher.UIThread.VerifyAccess();

        var context = new RenderContext(surface, _catalog, _controlCache,
            (surfaceId, eventName, payload) =>
                UserActionFired?.Invoke(this, new(surfaceId, eventName, payload)));

        // Render from root components
        var roots = surface.GetRootComponents().ToList();

        if (roots.Count == 1)
            return RenderComponent(roots[0], surface, context);

        var container = new StackPanel { Spacing = 8 };
        foreach (var root in roots)
            container.Children.Add(RenderComponent(root, surface, context));
        return container;
    }

    public event EventHandler<UserActionEventArgs>? UserActionFired;

    private Control RenderComponent(A2UiComponent component, Surface surface,
                                    RenderContext context)
    {
        if (!_catalog.TryGetEntry(component.Component, out var entry) || entry is null)
        {
            // Unknown type — render a placeholder
            return new TextBlock
            {
                Text    = $"[Unknown component: {component.Component}]",
                Classes = { "Caption" },
            };
        }

        // Try update in-place first (perf optimization)
        if (_controlCache.TryGetValue(component.Id, out var existing))
        {
            if (entry.Update(existing, component, surface.DataModel, context))
                return existing;
            // Update declined — fall through to recreate
        }

        var control = entry.Create(component, surface.DataModel, context);
        _controlCache[component.Id] = control;
        return control;
    }

    /// <summary>Clear control cache when surface is deleted.</summary>
    public void ClearSurface(string surfaceId) => _controlCache.Clear();
}

internal sealed class RenderContext(
    Surface surface,
    CatalogRegistry catalog,
    Dictionary<string, Control> cache,
    Action<string, string, object?> fireAction)
    : IRenderContext
{
    public Control? RenderChild(string? childId)
    {
        if (childId is null || !surface.Components.TryGetValue(childId, out var c))
            return null;

        if (!catalog.TryGetEntry(c.Component, out var entry) || entry is null)
            return null;

        if (cache.TryGetValue(c.Id, out var existing) &&
            entry.Update(existing, c, surface.DataModel, this))
            return existing;

        var control = entry.Create(c, surface.DataModel, this);
        cache[c.Id] = control;
        return control;
    }

    public IEnumerable<Control> RenderChildren(string parentId)
    {
        return surface.Components.Values
            .Where(c => c.Parent == parentId)
            .Select(c => RenderChild(c.Id))
            .OfType<Control>();
    }

    public void FireUserAction(string surfaceId, string eventName, object? payload = null) =>
        fireAction(surface.SurfaceId, eventName, payload);

    public string? Resolve(BoundOrLiteral? bound) =>
        surface.DataModel.Resolve(bound);
}

public sealed record UserActionEventArgs(
    string SurfaceId,
    string EventName,
    object? Payload);
```

---

## Phase 5 — Surface Host UserControl (Avalonia)

Create `src/A2Ui.Avalonia/Controls/A2UiSurface.cs`:

```csharp
using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace A2Ui.Avalonia.Controls;

/// <summary>
/// Avalonia UserControl that hosts an A2UI surface.
/// Bind <see cref="Surface"/> to update when the agent sends component updates.
/// </summary>
public sealed class A2UiSurface : ContentControl
{
    public static readonly StyledProperty<Surface?> SurfaceProperty =
        AvaloniaProperty.Register<A2UiSurface, Surface?>(nameof(Surface));

    private readonly A2UiRenderer _renderer;

    public A2UiSurface() : this(CatalogRegistry.CreateDefault()) { }

    public A2UiSurface(CatalogRegistry catalog)
    {
        _renderer = new A2UiRenderer(catalog);
        _renderer.UserActionFired += OnUserActionFired;
        this.GetObservable(SurfaceProperty).Subscribe(OnSurfaceChanged);
    }

    public Surface? Surface
    {
        get => GetValue(SurfaceProperty);
        set => SetValue(SurfaceProperty, value);
    }

    public event EventHandler<UserActionEventArgs>? UserActionFired;

    private void OnSurfaceChanged(Surface? surface)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (surface is null) { Content = null; return; }
            Content = _renderer.Render(surface);
        });
    }

    private void OnUserActionFired(object? sender, UserActionEventArgs e) =>
        UserActionFired?.Invoke(this, e);
}
```

---

## Phase 6 — In-Process Agent Event Bridge

Create `src/A2Ui.Avalonia/AgentEventBridge.cs`:

```csharp
using System.Text.Json;
using System.Threading.Channels;
using A2Ui.Core;
using A2Ui.Core.Messages;
using AgUi.Protocol;
using AgUi.Protocol.Events;
using Avalonia.Threading;

namespace A2Ui.Avalonia;

/// <summary>
/// In-process bridge between an AG-UI agent and the A2UI SurfaceManager.
/// Uses System.Threading.Channels for zero-serialization event passing.
/// Processes tool calls named "render_ui" or "update_surface" as A2UI messages.
/// </summary>
public sealed class AgentEventBridge : IDisposable
{
    private readonly Channel<BaseEvent>   _channel;
    private readonly SurfaceManager      _surfaceManager;
    private readonly ToolCallArgsAccumulator _accumulator = new();
    private CancellationTokenSource?     _cts;

    public AgentEventBridge(SurfaceManager surfaceManager, int capacity = 1024)
    {
        _surfaceManager = surfaceManager;
        _channel = Channel.CreateBounded<BaseEvent>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    /// <summary>Write an event from the agent (call from agent thread).</summary>
    public ValueTask WriteEventAsync(BaseEvent evt, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(evt, ct);

    /// <summary>Start processing events on a background task.</summary>
    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ProcessLoopAsync(_cts.Token));
    }

    public void Stop() => _cts?.Cancel();
    public void Dispose() => Stop();

    // Events surfaced to the app layer
    public event EventHandler<string>? AgentTextDelta;
    public event EventHandler?         RunStarted;
    public event EventHandler?         RunFinished;
    public event EventHandler<string>? RunError;

    private async Task ProcessLoopAsync(CancellationToken ct)
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync(ct))
        {
            switch (evt)
            {
                case RunStartedEvent:
                    Dispatcher.UIThread.Post(() => RunStarted?.Invoke(this, EventArgs.Empty));
                    break;

                case RunFinishedEvent:
                    Dispatcher.UIThread.Post(() => RunFinished?.Invoke(this, EventArgs.Empty));
                    break;

                case RunErrorEvent err:
                    Dispatcher.UIThread.Post(() => RunError?.Invoke(this, err.Message));
                    break;

                case TextMessageContentEvent tc:
                    Dispatcher.UIThread.Post(() => AgentTextDelta?.Invoke(this, tc.Delta));
                    break;

                case ToolCallArgsEvent args:
                    _accumulator.OnArgs(args);
                    break;

                case ToolCallEndEvent end:
                    string json = _accumulator.Complete(end.ToolCallId);
                    if (!string.IsNullOrWhiteSpace(json))
                        ProcessA2UiPayload(json);
                    break;
            }
        }
    }

    private void ProcessA2UiPayload(string json)
    {
        // A2UI payload is JSONL — one A2UiMessage per line
        foreach (var line in json.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var msg = JsonSerializer.Deserialize<A2UiMessage>(line.Trim());
                if (msg is not null)
                    Dispatcher.UIThread.Post(() => _surfaceManager.Process(msg));
            }
            catch (JsonException)
            {
                // Partial/malformed line — skip
            }
        }
    }
}
```

---

## Build & Commit

```bash
cd /sandbox/develop/A2Ui
dotnet build src/A2Ui.Avalonia/A2Ui.Avalonia.csproj
# Expected: 0 errors

git add src/A2Ui.Avalonia/
git commit -m "feat(renderer): implement Avalonia A2UI catalog and renderer engine

A2Ui.Avalonia:
- CatalogRegistry: type-string → ICatalogEntry factory mapping
- 13 built-in catalog entries covering basic_catalog.json types:
  Text, Button, Column, Row, Card, TextField, DateTimeInput,
  Select, Checkbox, Slider, Image, Table, Surface
- A2UiRenderer: depth-first tree render + in-place update optimization
- A2UiSurface: Avalonia UserControl hosting a Surface
- AgentEventBridge: in-process Channel<BaseEvent> → SurfaceManager

Architecture: zero-serialization in-process path for local LLMs.
First .NET/C# A2UI renderer (existing: Angular, Flutter, Lit, Markdown)."
```