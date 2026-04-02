---
name: a2ui_composer_port
description: Port A2UI Composer web app to Avalonia MVVM desktop: MainWindowViewModel, LocalNemotronAgent, MainWindow.axaml, MVVM best practices.
---


---

## Purpose

Port the A2UI Composer web application (`https://a2ui-composer.ag-ui.com/`)
to a native Avalonia UI MVVM desktop app.

The Composer is the reference UI for A2UI: it sends prompts to an agent,
receives AG-UI events including A2UI component trees, and renders them interactively.
This port makes it a first-class native .NET desktop application backed by
the local Nemotron 120B model on DGX Spark.

---

## Phase 1 — Study the Composer Web App

Before writing any Avalonia code, scan the Composer source in the A2UI repo:

```bash
cd /sandbox/develop/a2ui-reference

# Find Composer source
find . -name "composer" -type d
find . -path "*/composer/*" | grep -v node_modules | head -30

# Read key files
COMPOSER_DIR=$(find . -name "composer" -type d | head -1)
[ -n "$COMPOSER_DIR" ] && ls "$COMPOSER_DIR"

# Identify key features
find "$COMPOSER_DIR" -name "*.ts" -o -name "*.html" \
  | xargs grep -l "surface\|tool_call\|message\|history" 2>/dev/null | head -10
```

Document Composer features:
```bash
cat > /sandbox/develop/A2Ui/docs/composer-analysis.md << 'EOF'
# Composer Feature Analysis

## Core Features (from web app scan)
<!-- Fill after running scan above -->
1. Chat input / send button
2. Message history panel (scrollable)
3. A2UI surface render area
4. Tool call status indicator
5. Run status (started/finished/error)
6. Session management (thread ID, run ID)
7. Agent configuration (model, endpoint)

## Avalonia Port Plan
| Web Feature | Avalonia Implementation |
|-------------|------------------------|
| Chat input | TextBox + Button in DockPanel (Bottom) |
| Message history | ListBox / ItemsControl with templates |
| A2UI surface | A2UiSurface UserControl (skill 04) |
| Tool call status | StatusBar + ProgressBar |
| Run status | StatusBar indicator |
| Session | ViewModels with ObservableProperty |
| Agent config | Flyout / Settings panel |

## Layout
Main window → DockPanel
  Top:    ToolBar (session, config)
  Right:  A2UiSurface (main content)
  Left:   Chat panel (history + input)
  Bottom: StatusBar
EOF
```

---

## Phase 2 — Create the Avalonia App Project

```bash
cd /sandbox/develop/A2Ui

# Install Avalonia templates
dotnet new install Avalonia.Templates

# Create app project with MVVM template
dotnet new avalonia.mvvm -n A2Ui.Avalonia.App -o src/A2Ui.Avalonia.App \
  --framework net10.0

# Add to solution
dotnet sln add src/A2Ui.Avalonia.App/A2Ui.Avalonia.App.csproj
```

Edit `src/A2Ui.Avalonia.App/A2Ui.Avalonia.App.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <AssemblyName>A2Ui.Avalonia.App</AssemblyName>
    <RootNamespace>A2Ui.Avalonia.App</RootNamespace>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    <AvaloniaNameGeneratorIsEnabled>true</AvaloniaNameGeneratorIsEnabled>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" />
    <PackageReference Include="Avalonia.Desktop" />
    <PackageReference Include="Avalonia.Themes.Fluent" />
    <PackageReference Include="Avalonia.ReactiveUI" />
    <PackageReference Include="CommunityToolkit.Mvvm" />
    <PackageReference Include="Microsoft.Extensions.AI" />
    <PackageReference Include="Microsoft.Extensions.AI.OpenAI" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../A2Ui.Avalonia/A2Ui.Avalonia.csproj" />
    <ProjectReference Include="../AgUi.Protocol/AgUi.Protocol.csproj" />
    <ProjectReference Include="../A2Ui.Core/A2Ui.Core.csproj" />
  </ItemGroup>
</Project>
```

---

## Phase 3 — App Entry Point

Create `src/A2Ui.Avalonia.App/Program.cs`:

```csharp
using Avalonia;

namespace A2Ui.Avalonia.App;

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

Create `src/A2Ui.Avalonia.App/App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="A2Ui.Avalonia.App.App"
             RequestedThemeVariant="Dark">
  <Application.Styles>
    <FluentTheme />
  </Application.Styles>
</Application>
```

Create `src/A2Ui.Avalonia.App/App.axaml.cs`:

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using A2Ui.Avalonia.App.Views;
using A2Ui.Avalonia.App.ViewModels;

namespace A2Ui.Avalonia.App;

public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

---

## Phase 4 — ViewModels (CommunityToolkit.Mvvm)

Create `src/A2Ui.Avalonia.App/ViewModels/MessageViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace A2Ui.Avalonia.App.ViewModels;

public enum MessageRole { User, Assistant, ToolCall, System }

public sealed partial class MessageViewModel : ObservableObject
{
    [ObservableProperty] private string         _content = string.Empty;
    [ObservableProperty] private MessageRole    _role    = MessageRole.User;
    [ObservableProperty] private bool           _isStreaming;
    [ObservableProperty] private DateTimeOffset _timestamp = DateTimeOffset.UtcNow;

    public bool IsUser       => Role == MessageRole.User;
    public bool IsAssistant  => Role == MessageRole.Assistant;
    public bool IsToolCall   => Role == MessageRole.ToolCall;
}
```

Create `src/A2Ui.Avalonia.App/ViewModels/AgentConfigViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace A2Ui.Avalonia.App.ViewModels;

public sealed partial class AgentConfigViewModel : ObservableObject
{
    // Default: local Ollama on DGX Spark via SSH tunnel
    [ObservableProperty] private string _baseUrl  = "http://10.0.0.2:11434";
    [ObservableProperty] private string _model    = "nemotron-3-super:120b";
    [ObservableProperty] private string _endpoint = "http://localhost:8000"; // AG-UI server
    [ObservableProperty] private int    _maxTokens = 4096;
    [ObservableProperty] private double _temperature = 0.7;
}
```

Create `src/A2Ui.Avalonia.App/ViewModels/MainWindowViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using A2Ui.Avalonia;
using A2Ui.Core;
using A2Ui.Core.Messages;
using AgUi.Protocol;
using AgUi.Protocol.Events;
using AgUi.Protocol.Transport;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace A2Ui.Avalonia.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    // ── Bindable state ────────────────────────────────────────────────────
    [ObservableProperty] private string  _inputText   = string.Empty;
    [ObservableProperty] private bool    _isBusy;
    [ObservableProperty] private string  _statusText  = "Ready";
    [ObservableProperty] private string? _currentSurfaceId;
    [ObservableProperty] private Surface? _activeSurface;

    public ObservableCollection<MessageViewModel> Messages { get; } = [];
    public AgentConfigViewModel Config { get; } = new();

    // ── Infrastructure ────────────────────────────────────────────────────
    private readonly SurfaceManager    _surfaceManager = new();
    private readonly AgentEventBridge  _bridge;
    private          string            _threadId = Guid.NewGuid().ToString();
    private          string            _runId    = Guid.NewGuid().ToString();
    private          CancellationTokenSource? _runCts;

    public MainWindowViewModel()
    {
        _bridge = new AgentEventBridge(_surfaceManager);
        _bridge.AgentTextDelta += OnAgentTextDelta;
        _bridge.RunStarted     += OnRunStarted;
        _bridge.RunFinished    += OnRunFinished;
        _bridge.RunError       += OnRunError;
        _bridge.Start();

        _surfaceManager.SurfaceCreated   += OnSurfaceCreated;
        _surfaceManager.ComponentsUpdated += OnComponentsUpdated;
    }

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;

        string userText = InputText;
        InputText = string.Empty;

        Messages.Add(new MessageViewModel { Content = userText, Role = MessageRole.User });

        IsBusy    = true;
        StatusText = "Agent thinking…";
        _runId    = Guid.NewGuid().ToString();
        _runCts   = CancellationTokenSource.CreateLinkedTokenSource(ct);

        // Add streaming assistant placeholder
        var assistantMsg = new MessageViewModel
        {
            Role        = MessageRole.Assistant,
            IsStreaming = true,
        };
        Messages.Add(assistantMsg);

        try
        {
            await RunAgentAsync(userText, assistantMsg, _runCts.Token)
                .ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            assistantMsg.IsStreaming = false;
            StatusText = "Cancelled";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSend() => !IsBusy && !string.IsNullOrWhiteSpace(InputText);

    [RelayCommand]
    private void Cancel() => _runCts?.Cancel();

    [RelayCommand]
    private void NewSession()
    {
        _threadId = Guid.NewGuid().ToString();
        Messages.Clear();
        ActiveSurface = null;
        StatusText = "New session started";
    }

    // ── Agent execution ──────────────────────────────────────────────────

    private async Task RunAgentAsync(string userText, MessageViewModel streamTarget,
                                     CancellationToken ct)
    {
        var input = new RunAgentInput
        {
            ThreadId = _threadId,
            RunId    = _runId,
            Messages =
            [
                JsonSerializer.SerializeToElement(new { role = "user", content = userText }),
            ],
            State = JsonSerializer.SerializeToElement(new { }),
        };

        using var http = new System.Net.Http.HttpClient();
        http.Timeout = TimeSpan.FromMinutes(10);

        var request = new System.Net.Http.HttpRequestMessage(
            System.Net.Http.HttpMethod.Post, Config.Endpoint)
        {
            Content = new System.Net.Http.StringContent(
                JsonSerializer.Serialize(input),
                Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("text/event-stream");

        using var response = await http
            .SendAsync(request, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct)
            .ConfigureAwait(false);

        var sb = new StringBuilder();
        await foreach (var evt in SseEventParser.ParseAsync(stream, ct))
        {
            // Accumulate text for the streaming message
            if (evt is TextMessageContentEvent tc)
                sb.Append(tc.Delta);

            // Push to bridge for A2UI processing
            await _bridge.WriteEventAsync(evt, ct).ConfigureAwait(false);

            // Update streaming target on UI thread
            Dispatcher.UIThread.Post(() =>
            {
                streamTarget.Content = sb.ToString();
            });
        }

        streamTarget.IsStreaming = false;
    }

    // ── Event handlers ────────────────────────────────────────────────────

    private void OnAgentTextDelta(object? sender, string delta) { /* bridge handles */ }

    private void OnRunStarted(object? sender, EventArgs e) =>
        StatusText = "Agent running…";

    private void OnRunFinished(object? sender, EventArgs e) =>
        StatusText = "Ready";

    private void OnRunError(object? sender, string message) =>
        StatusText = $"Error: {message}";

    private void OnSurfaceCreated(object? sender, SurfaceCreatedEventArgs e)
    {
        CurrentSurfaceId = e.Surface.SurfaceId;
        ActiveSurface    = e.Surface;
    }

    private void OnComponentsUpdated(object? sender, ComponentsUpdatedEventArgs e)
    {
        if (e.Surface.SurfaceId == CurrentSurfaceId)
            ActiveSurface = e.Surface; // triggers binding refresh
    }

    public void Dispose()
    {
        _bridge.Dispose();
        _runCts?.Dispose();
    }
}
```

---

## Phase 5 — Main Window XAML

Create `src/A2Ui.Avalonia.App/Views/MainWindow.axaml`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:A2Ui.Avalonia.App.ViewModels"
        xmlns:a2ui="using:A2Ui.Avalonia.Controls"
        xmlns:views="using:A2Ui.Avalonia.App.Views"
        x:DataType="vm:MainWindowViewModel"
        x:Class="A2Ui.Avalonia.App.Views.MainWindow"
        Title="A2UI Composer — Avalonia"
        Width="1280" Height="800"
        MinWidth="900" MinHeight="600">

  <DockPanel>

    <!-- ── Top toolbar ─────────────────────────────────────── -->
    <ToolBar DockPanel.Dock="Top" Height="40">
      <Button Content="New Session"
              Command="{Binding NewSessionCommand}" />
      <Separator />
      <TextBlock Text="{Binding StatusText}"
                 VerticalAlignment="Center"
                 Margin="8,0" />
    </ToolBar>

    <!-- ── Bottom status bar ──────────────────────────────── -->
    <StatusBar DockPanel.Dock="Bottom" Height="24">
      <StatusBarItem>
        <TextBlock Text="{Binding StatusText}" />
      </StatusBarItem>
      <StatusBarItem HorizontalAlignment="Right">
        <ProgressBar IsIndeterminate="{Binding IsBusy}"
                     IsVisible="{Binding IsBusy}"
                     Width="120" Height="14" />
      </StatusBarItem>
    </StatusBar>

    <!-- ── Main content: Chat | Surface ───────────────────── -->
    <Grid ColumnDefinitions="400,*">

      <!-- Left: Chat panel -->
      <DockPanel Grid.Column="0" Margin="8">

        <!-- Input area -->
        <Grid DockPanel.Dock="Bottom" RowDefinitions="Auto,Auto"
              Margin="0,8,0,0">
          <TextBox Grid.Row="0"
                   Text="{Binding InputText}"
                   Watermark="Message the agent…"
                   AcceptsReturn="False"
                   MaxHeight="120"
                   TextWrapping="Wrap">
            <TextBox.KeyBindings>
              <KeyBinding Gesture="Enter"
                          Command="{Binding SendCommand}" />
            </TextBox.KeyBindings>
          </TextBox>
          <Grid Grid.Row="1" ColumnDefinitions="*,Auto" Margin="0,4,0,0">
            <Button Grid.Column="0"
                    Content="Send"
                    Command="{Binding SendCommand}"
                    Classes="accent"
                    HorizontalAlignment="Stretch" />
            <Button Grid.Column="1"
                    Content="Stop"
                    Command="{Binding CancelCommand}"
                    IsVisible="{Binding IsBusy}"
                    Margin="4,0,0,0" />
          </Grid>
        </Grid>

        <!-- Message history -->
        <ScrollViewer VerticalScrollBarVisibility="Auto">
          <ItemsControl ItemsSource="{Binding Messages}">
            <ItemsControl.ItemTemplate>
              <DataTemplate DataType="vm:MessageViewModel">
                <views:MessageBubble DataContext="{Binding}" Margin="0,4" />
              </DataTemplate>
            </ItemsControl.ItemTemplate>
          </ItemsControl>
        </ScrollViewer>

      </DockPanel>

      <!-- Right: A2UI Surface render area -->
      <Border Grid.Column="1"
              Margin="4,8,8,8"
              CornerRadius="8"
              Classes="Card">
        <ScrollViewer HorizontalScrollBarVisibility="Auto"
                      VerticalScrollBarVisibility="Auto">
          <a2ui:A2UiSurface Surface="{Binding ActiveSurface}"
                             Margin="16" />
        </ScrollViewer>
      </Border>

    </Grid>
  </DockPanel>
</Window>
```

---

## Phase 6 — Message Bubble UserControl

Create `src/A2Ui.Avalonia.App/Views/MessageBubble.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:A2Ui.Avalonia.App.ViewModels"
             x:DataType="vm:MessageViewModel"
             x:Class="A2Ui.Avalonia.App.Views.MessageBubble">
  <Border CornerRadius="8"
          Padding="12,8"
          MaxWidth="360"
          HorizontalAlignment="{Binding IsUser, Converter={x:Static BoolConverters.ToAlignmentConverter}}"
          Background="{Binding IsUser,
            Converter={x:Static BoolConverters.ToObjectConverter},
            ConverterParameter='#1e40af|#374151'}">
    <StackPanel Spacing="4">
      <TextBlock Text="{Binding Content}"
                 TextWrapping="Wrap"
                 Foreground="White"
                 FontSize="14" />
      <TextBlock Text="{Binding Timestamp, StringFormat='HH:mm'}"
                 FontSize="11"
                 Opacity="0.6"
                 Foreground="White"
                 HorizontalAlignment="Right" />
    </StackPanel>
  </Border>
</UserControl>
```

---

## Phase 7 — Agent Config (Local Nemotron)

The app can connect to the AG-UI server OR directly to Ollama.
For local Nemotron via Ollama on DGX Spark:

Create `src/A2Ui.Avalonia.App/Agent/LocalNemotronAgent.cs`:

```csharp
using System.Text.Json;
using System.Threading.Channels;
using A2Ui.Core;
using AgUi.Protocol;
using AgUi.Protocol.Events;
using Microsoft.Extensions.AI;

namespace A2Ui.Avalonia.App.Agent;

/// <summary>
/// Direct in-process AG-UI agent backed by local Ollama (Nemotron 120B).
/// Bypasses HTTP/SSE entirely — uses in-process Channel for zero latency.
/// </summary>
public sealed class LocalNemotronAgent(string ollamaBaseUrl, string modelId)
{
    private readonly IChatClient _client = new OpenAI.Chat.ChatClient(
            modelId,
            new System.ClientModel.ApiKeyCredential("ollama"),
            new OpenAI.OpenAIClientOptions { Endpoint = new Uri($"{ollamaBaseUrl}/v1") })
        .AsIChatClient();

    /// <summary>
    /// Run the agent and write AG-UI events to the provided channel writer.
    /// </summary>
    public async Task RunAsync(
        RunAgentInput input,
        ChannelWriter<BaseEvent> events,
        CancellationToken ct = default)
    {
        string runId    = input.RunId;
        string threadId = input.ThreadId;

        await events.WriteAsync(new RunStartedEvent
            { ThreadId = threadId, RunId = runId }, ct).ConfigureAwait(false);

        // Build message history from input
        var messages = input.Messages
            .Select(m => new ChatMessage(
                m.GetProperty("role").GetString() == "user"
                    ? ChatRole.User : ChatRole.Assistant,
                m.GetProperty("content").GetString() ?? string.Empty))
            .ToList();

        string messageId = Guid.NewGuid().ToString("N");

        await events.WriteAsync(new TextMessageStartEvent
            { MessageId = messageId }, ct).ConfigureAwait(false);

        var fullResponse = new System.Text.StringBuilder();

        await foreach (var update in _client
            .GetStreamingResponseAsync(messages, cancellationToken: ct))
        {
            foreach (var content in update.Contents)
            {
                if (content is TextContent tc && !string.IsNullOrEmpty(tc.Text))
                {
                    fullResponse.Append(tc.Text);
                    await events.WriteAsync(new TextMessageContentEvent
                        { MessageId = messageId, Delta = tc.Text }, ct).ConfigureAwait(false);
                }
            }
        }

        await events.WriteAsync(new TextMessageEndEvent
            { MessageId = messageId }, ct).ConfigureAwait(false);

        await events.WriteAsync(new RunFinishedEvent
            { ThreadId = threadId, RunId = runId }, ct).ConfigureAwait(false);

        events.Complete();
    }
}
```

---

## Phase 8 — Build and Run

```bash
cd /sandbox/develop/A2Ui
dotnet build src/A2Ui.Avalonia.App/A2Ui.Avalonia.App.csproj

# Run (requires display; set DISPLAY or use headless mode for CI)
DISPLAY=:0 dotnet run --project src/A2Ui.Avalonia.App/A2Ui.Avalonia.App.csproj
```

For headless execution on DGX Spark (no display):

```bash
# Use Xvfb virtual display
Xvfb :99 -screen 0 1280x800x24 &
DISPLAY=:99 dotnet run --project src/A2Ui.Avalonia.App/A2Ui.Avalonia.App.csproj
```

---

## Phase 9 — Commit

```bash
git add src/A2Ui.Avalonia.App/
git commit -m "feat(app): port A2UI Composer to Avalonia MVVM desktop

A2Ui.Avalonia.App:
- MainWindowViewModel: full agent lifecycle with CancellationToken
  Observable properties via CommunityToolkit.Mvvm source generators
  SendCommand, CancelCommand, NewSessionCommand
- AgentConfigViewModel: Ollama base URL + model + endpoint
- MessageViewModel: role-aware chat message with streaming flag
- LocalNemotronAgent: in-process IChatClient → Channel<BaseEvent>
  Direct Ollama v1/chat/completions API (no SSE server needed)
- MainWindow.axaml: DockPanel layout, chat panel, A2UiSurface host
- MessageBubble.axaml: role-aligned chat bubbles

Architecture:
- SSE mode: connect to any AG-UI server endpoint
- In-process mode: LocalNemotronAgent → AgentEventBridge → SurfaceManager
- Zero overhead for local DGX Spark deployments

License: Avalonia (MIT), CommunityToolkit.Mvvm (MIT), xUnit (Apache 2.0)"
```

---

## Phase 10 — MVVM Best Practices Checklist

Apply these principles throughout all ViewModel work:

```
✅ All properties use [ObservableProperty] source generator (no manual INPC)
✅ All commands use [RelayCommand] with CanExecute
✅ No UI types (Control, Window) in ViewModels — ViewModels are testable POCOs
✅ No async void (except event handlers — always try/catch)
✅ CancellationToken threaded through all async operations
✅ ConfigureAwait(false) on all awaits in library code
✅ ConfigureAwait(true) in ViewModel commands (need to return to UI thread)
✅ IDisposable on ViewModels that hold resources (bridge, CTS)
✅ Services injected via constructor (dependency injection ready)
✅ ViewModels have xUnit tests (no Avalonia required)
✅ Complex UI logic tested with Avalonia.Headless.XUnit
✅ No magic strings — use nameof() or constants
✅ No nullable reference suppression (!) without comment explaining why
```