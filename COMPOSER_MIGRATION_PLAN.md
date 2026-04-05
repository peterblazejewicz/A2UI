# Composer Migration Plan — .NET Avalonia UI Desktop App

## Overview

Port the A2UI Composer web application to a native Avalonia UI MVVM desktop app.
The app connects to an AG-UI agent (via HTTP/SSE or in-process), receives events
including A2UI component trees, and renders them interactively using the
`A2Ui.Avalonia` renderer library built in previous phases.

**Source reference:** `tools/composer/` (Next.js/React web app)
**Target location:** `samples/client/avalonia/composer/`
**Existing scaffold:** `A2Ui.Avalonia.Composer.csproj` (dependencies configured, placeholder only)

---

## Prerequisites

Before starting this plan, verify:

```bash
# All SDK tests pass
cd agent_sdks/dotnet
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test A2Ui.sln --configuration Release

# Renderer tests pass
cd renderers/avalonia
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/A2Ui.Avalonia.Tests/A2Ui.Avalonia.Tests.csproj --configuration Release
```

**Required library state (all implemented):**
- `AgUi.Protocol` — 28 event types, `SseEventParser`, `ToolCallArgsAccumulator`
- `A2Ui.Core` — `DynamicValue`, `ChildList`, `SurfaceManager` (events outside lock), `DataModel` (array paths)
- `A2Ui.Avalonia` — 18 catalog entries, `A2UiRenderer` (per-surface cache), `A2UiSurface` (`Refresh()`), `AgentEventBridge` (tool name filter, `UserActionReceived`)

---

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    A2Ui.Avalonia.Composer                     │
│                                                              │
│  ┌──────────────┐    ┌─────────────────┐    ┌────────────┐  │
│  │ MainWindow   │    │ MainWindowVM    │    │ Agent      │  │
│  │ .axaml       │◄──►│ (Commands,      │◄──►│ HttpClient │  │
│  │              │    │  State,         │    │ or Local   │  │
│  │ ┌──────────┐ │    │  Messages)      │    │ Nemotron   │  │
│  │ │ChatPanel │ │    └────────┬────────┘    └────────────┘  │
│  │ │(Messages)│ │             │                              │
│  │ └──────────┘ │    ┌────────▼────────┐                    │
│  │ ┌──────────┐ │    │ AgentEvent      │                    │
│  │ │A2UiSurf- │ │    │ Bridge          │                    │
│  │ │ace       │◄├────│ (Channel→       │                    │
│  │ │(Renderer)│ │    │  SurfaceManager)│                    │
│  │ └──────────┘ │    └─────────────────┘                    │
│  └──────────────┘                                            │
└──────────────────────────────────────────────────────────────┘
         │                    │                     │
         ▼                    ▼                     ▼
   A2Ui.Avalonia         A2Ui.Core          AgUi.Protocol
   (Catalog,Render)      (Surface,DataModel) (Events,SSE)
```

**Two agent modes:**
- **SSE mode:** HTTP POST to AG-UI server → SSE stream → `SseEventParser` → Bridge
- **In-process mode:** `LocalNemotronAgent` → `Channel<BaseEvent>` → Bridge (zero latency)

---

## Coding Standards (from CLAUDE.md)

- `Nullable` enabled, `TreatWarningsAsErrors` true
- File-scoped namespaces: `namespace X.Y;`
- `sealed record` for immutable data, `sealed class` for services
- `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm
- `ConfigureAwait(false)` in library code, `ConfigureAwait(true)` in ViewModel commands
- `CancellationToken` threaded through all async signatures
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Commit format: `feat(avalonia-app): <description>`

---

## Step 1: App Scaffold

**Goal:** A buildable, launchable Avalonia desktop app with an empty window.

**Files to create:**
```
samples/client/avalonia/composer/
├── Program.cs
├── App.axaml
├── App.axaml.cs
└── Views/
    └── MainWindow.axaml
    └── MainWindow.axaml.cs
```

**Modify:** `A2Ui.Avalonia.Composer.csproj`
- Change `Avalonia.Themes.Default` → `Avalonia.Themes.Fluent` (matches renderer)
- Add `Avalonia.Fonts.Inter` if not present

**Delete:** `__placeholder.txt`

### Program.cs
```csharp
namespace A2Ui.Avalonia.Composer;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
                  .UsePlatformDetect()
                  .WithInterFont()
                  .LogToTrace();
}
```

### App.axaml
```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="A2Ui.Avalonia.Composer.App"
             RequestedThemeVariant="Dark">
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>
</Application>
```

### App.axaml.cs
```csharp
namespace A2Ui.Avalonia.Composer;

public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

### MainWindow.axaml
Minimal — just a title and "A2UI Composer" text to prove it launches.

### Verification
```bash
cd samples/client/avalonia/composer
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release
# If display available:
# DISPLAY=:0 dotnet run
```

### Commit
```
feat(avalonia-app): scaffold Composer desktop app

- Program.cs with Avalonia desktop lifetime
- App.axaml with FluentTheme (Dark)
- Empty MainWindow proving the app builds and launches
- Remove __placeholder.txt
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 2: Data ViewModels

**Goal:** Pure C# ViewModels for messages and agent config. Testable without Avalonia.

**Files to create:**
```
samples/client/avalonia/composer/
├── ViewModels/
│   ├── MessageViewModel.cs
│   └── AgentConfigViewModel.cs
```

### MessageViewModel.cs
```csharp
namespace A2Ui.Avalonia.Composer.ViewModels;

public enum MessageRole { User, Assistant, ToolCall, System }

public sealed partial class MessageViewModel : ObservableObject
{
    [ObservableProperty] private string         _content     = string.Empty;
    [ObservableProperty] private MessageRole    _role        = MessageRole.User;
    [ObservableProperty] private bool           _isStreaming;
    [ObservableProperty] private DateTimeOffset _timestamp   = DateTimeOffset.UtcNow;

    public bool IsUser      => Role == MessageRole.User;
    public bool IsAssistant => Role == MessageRole.Assistant;
}
```

### AgentConfigViewModel.cs
```csharp
namespace A2Ui.Avalonia.Composer.ViewModels;

public sealed partial class AgentConfigViewModel : ObservableObject
{
    [ObservableProperty] private string _baseUrl     = "http://localhost:11434";
    [ObservableProperty] private string _model       = "nemotron-3-super:120b";
    [ObservableProperty] private string _endpoint    = "http://localhost:8000";
    [ObservableProperty] private int    _maxTokens   = 4096;
    [ObservableProperty] private double _temperature = 0.7;
    [ObservableProperty] private bool   _useLocalAgent = true;
}
```

### Tests
Create `samples/client/avalonia/composer/Tests/ViewModelTests.cs` (or a separate test project).

Test that:
- `MessageViewModel` properties notify on change
- `IsUser`/`IsAssistant` computed properties reflect `Role`
- `AgentConfigViewModel` has correct defaults

### Commit
```
feat(avalonia-app): add MessageViewModel and AgentConfigViewModel

- MessageViewModel: Content, Role, IsStreaming, Timestamp with INPC
- AgentConfigViewModel: BaseUrl, Model, Endpoint, UseLocalAgent
- xUnit tests for property change notification and defaults
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 3: MainWindowViewModel Shell

**Goal:** Core ViewModel with commands and SurfaceManager/Bridge wiring. No agent execution yet.

**Files to create:**
```
samples/client/avalonia/composer/
├── ViewModels/
│   └── MainWindowViewModel.cs
```

### MainWindowViewModel.cs

Key elements:
- `[ObservableProperty]` fields: `InputText`, `IsBusy`, `StatusText`, `CurrentSurfaceId`, `ActiveSurface`
- `ObservableCollection<MessageViewModel> Messages`
- `AgentConfigViewModel Config`
- `SurfaceManager` + `AgentEventBridge` instantiation
- `[RelayCommand]` `SendAsync` — stub that adds a user message to `Messages` (no agent call yet)
- `[RelayCommand]` `Cancel` — cancels `_runCts`
- `[RelayCommand]` `NewSession` — clears messages, resets thread ID
- `IDisposable` — disposes bridge and CTS
- Event handlers: `OnSurfaceCreated`, `OnComponentsUpdated`, `OnDataModelUpdated`

**Critical implementation note:** `OnComponentsUpdated` must NOT set `ActiveSurface = e.Surface`
(same reference won't trigger property change). Instead, it should raise a custom event or
use a `SurfaceVersion` counter that the View monitors. The View code-behind calls
`A2UiSurface.Refresh()` in response. See Step 4.

### Tests

Test with xUnit (no Avalonia):
- `NewSession_ClearsMessagesAndResetsState`
- `SendAsync_EmptyInput_DoesNothing`
- `SendAsync_AddsUserMessage`
- `Cancel_CancelsCts`
- `Dispose_DisposesResources`

### Commit
```
feat(avalonia-app): add MainWindowViewModel with commands and surface wiring

- SendAsync (stub), CancelCommand, NewSessionCommand
- SurfaceManager + AgentEventBridge lifecycle
- Event handlers for surface create/update/delete
- IDisposable pattern for resource cleanup
- xUnit tests for all commands
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 4: MainWindow Layout + MessageBubble

**Goal:** Full window layout with chat panel, surface area, toolbar, and status bar.

**Files to create:**
```
samples/client/avalonia/composer/
├── Views/
│   ├── MainWindow.axaml       (rewrite from Step 1 stub)
│   ├── MainWindow.axaml.cs    (code-behind: Refresh wiring)
│   └── MessageBubble.axaml
│   └── MessageBubble.axaml.cs
```

### MainWindow.axaml Layout
```
DockPanel
  Top:    ToolBar (New Session button, status text)
  Bottom: StatusBar (status text, busy indicator)
  Content: Grid (2 columns)
    Left (400px):  DockPanel
      Bottom: Input area (TextBox + Send/Stop buttons)
      Fill:   ScrollViewer → ItemsControl (Messages, template: MessageBubble)
    Right (*):     Border.Card → ScrollViewer → A2UiSurface
```

### MainWindow.axaml.cs (Code-Behind)

**Critical:** This is where `A2UiSurface.Refresh()` is wired. The ViewModel exposes
`SurfaceManager` (or a `SurfaceUpdated` event). The code-behind subscribes to
`SurfaceManager.ComponentsUpdated` / `DataModelUpdated` and calls the `A2UiSurface.Refresh()`.

Also wire: `a2uiSurface.UserActionFired += bridge.OnUserAction;`

```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainWindowViewModel vm)
        {
            // Wire surface refresh — ViewModel can't touch UI controls
            vm.SurfaceManager.ComponentsUpdated += (_, _) =>
                this.FindControl<A2UiSurface>("Surface")?.Refresh();
            vm.SurfaceManager.DataModelUpdated += (_, _) =>
                this.FindControl<A2UiSurface>("Surface")?.Refresh();

            // Wire UserAction round-trip
            var surface = this.FindControl<A2UiSurface>("Surface");
            if (surface is not null)
                surface.UserActionFired += vm.Bridge.OnUserAction;
        }
    }
}
```

### MessageBubble.axaml
```xml
<Border CornerRadius="8" Padding="12,8" MaxWidth="360">
  <StackPanel Spacing="4">
    <TextBlock Text="{Binding Content}" TextWrapping="Wrap" />
    <TextBlock Text="{Binding Timestamp, StringFormat='HH:mm'}"
               FontSize="11" Opacity="0.6" HorizontalAlignment="Right" />
  </StackPanel>
</Border>
```

Style by Role: User messages right-aligned with accent color, Assistant messages
left-aligned with neutral color. Use `Classes` and Avalonia styles.

### Verification
```bash
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release
# Visual test: launch app, type text, press Send, see message bubble appear
```

### Commit
```
feat(avalonia-app): add MainWindow layout and MessageBubble

- DockPanel layout: toolbar, statusbar, chat panel, surface area
- MessageBubble UserControl with role-based alignment
- Code-behind wires A2UiSurface.Refresh() to SurfaceManager events
- Code-behind wires UserActionFired → AgentEventBridge.OnUserAction
- Avalonia.Headless smoke test
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 5: AgentHttpClient (SSE Transport)

**Goal:** Reusable HTTP+SSE transport in `AgUi.Protocol.Transport`.

**File to create:**
```
agent_sdks/dotnet/src/AgUi.Protocol/Transport/AgentHttpClient.cs
```

### AgentHttpClient.cs
```csharp
namespace AgUi.Protocol.Transport;

public sealed class AgentHttpClient : IDisposable
{
    private readonly HttpClient _http;

    public AgentHttpClient(Uri? baseAddress = null)
    {
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        if (baseAddress is not null)
            _http.BaseAddress = baseAddress;
    }

    public async IAsyncEnumerable<BaseEvent> RunAsync(
        Uri endpoint,
        RunAgentInput input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(input);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("text/event-stream");

        using var response = await _http
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content
            .ReadAsStreamAsync(ct).ConfigureAwait(false);

        await foreach (var evt in SseEventParser.ParseAsync(stream, ct))
            yield return evt;
    }

    public void Dispose() => _http.Dispose();
}
```

### Tests
In `agent_sdks/dotnet/tests/AgUi.Protocol.Tests/Transport/AgentHttpClientTests.cs`:

- `RunAsync_WellFormedStream_YieldsEvents` — use `MemoryStream` with SSE data, mock `HttpMessageHandler`
- Verify events are correctly typed via `SseEventParser`

### Commit
```
feat(dotnet-sdk): add AgentHttpClient for reusable SSE transport

- POST RunAgentInput to endpoint, return IAsyncEnumerable<BaseEvent>
- Uses SseEventParser internally
- Configurable timeout, proper IDisposable
- xUnit tests with mock HTTP handler
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 6: Agent Execution Wiring

**Goal:** MainWindowViewModel connects to an agent, streams responses, updates UI.

**Modify:** `MainWindowViewModel.cs`

### Implementation

Replace the `SendAsync` stub with real agent execution:

```csharp
[RelayCommand(CanExecute = nameof(CanSend))]
private async Task SendAsync(CancellationToken ct)
{
    string userText = InputText;
    InputText = string.Empty;
    Messages.Add(new MessageViewModel { Content = userText, Role = MessageRole.User });

    IsBusy = true;
    StatusText = "Agent thinking...";
    _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

    var assistantMsg = new MessageViewModel
    {
        Role = MessageRole.Assistant,
        IsStreaming = true,
    };
    Messages.Add(assistantMsg);

    try
    {
        await RunAgentAsync(userText, assistantMsg, _runCts.Token)
            .ConfigureAwait(true);
    }
    catch (OperationCanceledException) { StatusText = "Cancelled"; }
    catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
    finally
    {
        assistantMsg.IsStreaming = false;
        IsBusy = false;
    }
}
```

`RunAgentAsync` feeds events into `AgentEventBridge.WriteEventAsync` and accumulates
text deltas into `assistantMsg.Content` via `Dispatcher.UIThread.Post`.

For SSE mode: use `AgentHttpClient.RunAsync`.
For in-process mode: pipe events from `LocalNemotronAgent` (Step 7).

### Message History

Build `RunAgentInput.Messages` from the full `Messages` collection (not just the
current message). Filter to User and Assistant roles, serialize as `JsonElement[]`.

### Tests

- `RunAgentAsync_StreamsTextToAssistantMessage` (with mock event source)
- `RunAgentAsync_CancellationToken_StopsProcessing`

### Commit
```
feat(avalonia-app): wire agent execution and streaming text display

- SendAsync: full lifecycle with cancellation, error handling
- RunAgentAsync: feeds events through Bridge → SurfaceManager
- Message history accumulation for multi-turn context
- Streaming text display via Dispatcher.UIThread.Post
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 7: LocalNemotronAgent

**Goal:** In-process agent backed by local Ollama via `Microsoft.Extensions.AI`.

**File to create:**
```
samples/client/avalonia/composer/
├── Agent/
│   └── LocalNemotronAgent.cs
```

**Modify csproj:** Add `Microsoft.Extensions.AI` and `Microsoft.Extensions.AI.OpenAI` packages.

### LocalNemotronAgent.cs

The agent:
1. Creates an `IChatClient` via OpenAI-compatible Ollama endpoint
2. Accepts `RunAgentInput` + `ChannelWriter<BaseEvent>`
3. Emits AG-UI event sequence: `RUN_STARTED` → `TEXT_MESSAGE_START` → streaming `TEXT_MESSAGE_CONTENT` → `TEXT_MESSAGE_END` → `RUN_FINISHED`
4. Calls `events.Complete()` when done

```csharp
namespace A2Ui.Avalonia.Composer.Agent;

public sealed class LocalNemotronAgent(string ollamaBaseUrl, string modelId)
{
    public async Task RunAsync(
        RunAgentInput input,
        ChannelWriter<BaseEvent> events,
        CancellationToken ct = default)
    {
        // Build IChatClient from Ollama endpoint
        // Emit AG-UI events as streaming response arrives
        // Handle tool calls for A2UI rendering (future enhancement)
    }
}
```

### Wire to MainWindowViewModel

When `Config.UseLocalAgent` is true, `RunAgentAsync` creates a local
`Channel<BaseEvent>`, starts `LocalNemotronAgent.RunAsync` on a background task,
and feeds events from the channel reader into `AgentEventBridge.WriteEventAsync`.

### Tests

Test with a mock `IChatClient`:
- `RunAsync_EmitsCorrectEventSequence`
- `RunAsync_StreamsTextContent`
- `RunAsync_Cancellation_CompletesChannel`

### Commit
```
feat(avalonia-app): add LocalNemotronAgent for in-process Ollama

- IChatClient via Microsoft.Extensions.AI.OpenAI
- AG-UI event emission: RUN_STARTED → TEXT_MESSAGE_* → RUN_FINISHED
- Channel-based output for zero-latency AgentEventBridge integration
- Toggle via AgentConfigViewModel.UseLocalAgent
- xUnit tests with mock IChatClient
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 8: UserAction Round-Trip + Context Resolution

**Goal:** Button clicks and input changes in rendered surfaces reach the agent.

### Implementation

In `MainWindowViewModel`, subscribe to `Bridge.UserActionReceived`:

```csharp
_bridge.UserActionReceived += OnUserActionReceived;

private void OnUserActionReceived(object? sender, UserActionEventArgs e)
{
    // Build ClientAction from UserActionEventArgs
    var action = new ClientAction
    {
        Name = e.EventName,
        SurfaceId = e.SurfaceId,
        SourceComponentId = "unknown", // TODO: propagate from button
        Timestamp = DateTimeOffset.UtcNow.ToString("O"),
        Context = JsonSerializer.SerializeToElement(e.Payload ?? new { }),
    };

    // Add to message history as a tool-call message
    Messages.Add(new MessageViewModel
    {
        Role = MessageRole.ToolCall,
        Content = $"Action: {e.EventName}",
    });

    // For SSE mode: POST ClientToServerMessage to agent endpoint
    // For in-process mode: inject into agent context for next turn
}
```

### Tests

- `OnUserActionReceived_AddsToolCallMessage`
- `OnUserActionReceived_BuildsClientAction`

### Commit
```
feat(avalonia-app): wire UserAction round-trip to agent

- Subscribe to AgentEventBridge.UserActionReceived
- Build ClientAction from UserActionEventArgs
- Add tool-call messages to chat history
- Serialize ClientToServerMessage for agent feedback
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Step 9: Polish, Error Handling, and Documentation

**Goal:** Production-quality error handling, edge cases, and documentation.

### Error handling
- Network errors in `AgentHttpClient` — show in StatusText, don't crash
- Ollama not running — clear error message with retry suggestion
- Invalid agent response — log and skip (resilient parsing already in SseEventParser)
- Surface creation failure — show placeholder

### Edge cases
- Send while previous run is still active (disable button via CanSend)
- Rapid New Session during active run (cancel first)
- Multiple surfaces (show most recent, keep others in background)
- Empty message history on first turn

### UI polish
- Auto-scroll chat to bottom on new message
- Keyboard shortcut: Enter to send (already in XAML)
- Show agent model name in toolbar
- Busy indicator during agent execution

### Documentation updates
- Update `CLAUDE.md` Build & Test section with Composer commands:
  ```
  cd samples/client/avalonia/composer
  DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet build --configuration Release
  ```
- Update Repository Structure in CLAUDE.md

### Commit
```
feat(avalonia-app): polish error handling, edge cases, documentation

- Network error display in StatusText
- Auto-scroll chat to bottom
- Busy indicator UX
- CLAUDE.md updated with Composer build commands
```

**Status:** `[ ] pending` | **Commit:** _to be filled_

---

## Summary

| Step | Deliverable | Key Files | Test Type |
|------|-------------|-----------|-----------|
| 1 | App scaffold | Program.cs, App.axaml, MainWindow | Build only |
| 2 | Data ViewModels | MessageViewModel, AgentConfigViewModel | xUnit |
| 3 | MainWindowViewModel shell | Commands, state, Surface wiring | xUnit |
| 4 | MainWindow layout | XAML, MessageBubble, code-behind wiring | Headless |
| 5 | AgentHttpClient | SSE transport in AgUi.Protocol | xUnit |
| 6 | Agent execution | RunAgentAsync, streaming, history | xUnit |
| 7 | LocalNemotronAgent | In-process Ollama agent | xUnit |
| 8 | UserAction round-trip | ClientAction, context resolution | xUnit |
| 9 | Polish | Error handling, docs, edge cases | Manual |

**Estimated total:** ~1,500-2,000 LoC across ~15 files, plus tests.

**Dependency chain:** Steps 1→2→3→4 are sequential (each builds on the previous).
Steps 5 and 7 are semi-independent (transport and agent, respectively).
Step 6 requires Steps 3+5. Step 8 requires Steps 4+6.

---

## Known Limitations (Deferred)

These are documented gaps that will not be addressed in this plan:

- **FunctionCall resolution** — `DynamicValue.FunctionCall` returns null from `DataModel.Resolve`. Components using function calls (formatDate, required, etc.) will show blank values.
- **Template children expansion** — `ChildList.Template` renders one item, not N. Dynamic lists from data model arrays are not expanded.
- **Multi-surface management** — The app shows one surface at a time. No surface switcher or tab view.
- **Agent tool calling** — `LocalNemotronAgent` emits text only. To render A2UI surfaces, the agent must be prompted to emit tool calls with A2UI JSONL payloads. This requires prompt engineering or a dedicated A2UI agent framework, which is outside the scope of this port.
