using System.Collections.ObjectModel;
using A2Ui.Avalonia.Shell.Services;
using A2Ui.Core;
using A2Ui.Core.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace A2Ui.Avalonia.Shell.ViewModels;

public sealed partial class ShellViewModel : ObservableObject, IDisposable
{
    private readonly IA2AClient _client;
    private readonly SurfaceManager _manager;
    private readonly ILogger<ShellViewModel> _logger;

    public ShellViewModel(IA2AClient client, SurfaceManager manager, ILogger<ShellViewModel> logger)
    {
        _client = client;
        _manager = manager;
        _logger = logger;

        _manager.SurfaceCreated += OnSurfaceCreated;
        _manager.SurfaceDeleted += OnSurfaceDeleted;
        _manager.ComponentsUpdated += OnComponentsUpdated;
        _manager.DataModelUpdated += OnDataModelUpdated;
    }

    // ── State ──────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private string _promptText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Connecting...";

    [ObservableProperty]
    private string? _errorText;

    public ObservableCollection<Surface> Surfaces { get; } = [];

    // ── Commands ──────────────────────────────────────────

    private bool CanSend => !IsBusy && !string.IsNullOrWhiteSpace(PromptText);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync(CancellationToken ct)
    {
        string query = PromptText.Trim();
        PromptText = string.Empty;
        ErrorText = null;

        IsBusy = true;
        try
        {
            _manager.Clear();

            IReadOnlyList<A2UiMessage> messages = await _client.SendTextAsync(query, ct).ConfigureAwait(true);

            foreach (A2UiMessage msg in messages)
                _manager.Process(msg);

            _logger.LogInformation("Processed {Count} message(s) for query: {Query}", messages.Count, query);
        }
        catch (OperationCanceledException)
        {
            // Cancelled — nothing to do
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to send query to agent");
            ErrorText = $"Connection error: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending query");
            ErrorText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Action dispatch (Slice 6 wires this) ─────────────

    public async Task HandleUserActionAsync(UserActionEventArgs e, CancellationToken ct = default)
    {
        if (IsBusy)
            return;

        ErrorText = null;
        IsBusy = true;
        try
        {
            object envelope = UserActionSerializer.Serialize(e);
            _logger.LogInformation("Dispatching action: {Action} on {Surface}", e.EventName, e.SurfaceId);

            _manager.Clear();

            IReadOnlyList<A2UiMessage> messages = await _client.SendActionAsync(envelope, ct).ConfigureAwait(true);

            foreach (A2UiMessage msg in messages)
                _manager.Process(msg);

            _logger.LogInformation("Processed {Count} message(s) for action: {Action}", messages.Count, e.EventName);
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to send action to agent");
            ErrorText = $"Connection error: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending action");
            ErrorText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── Initialization ────────────────────────────────────

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            string name = await _client.GetAgentNameAsync(ct).ConfigureAwait(true);
            StatusText = name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch agent card");
            StatusText = "Agent (offline)";
        }
    }

    // ── View event ────────────────────────────────────────

    public event EventHandler? SurfaceRefreshRequested;

    // ── SurfaceManager event handlers ─────────────────────

    private void OnSurfaceCreated(object? sender, SurfaceCreatedEventArgs e) => Surfaces.Add(e.Surface);

    private void OnSurfaceDeleted(object? sender, SurfaceDeletedEventArgs e)
    {
        for (int i = Surfaces.Count - 1; i >= 0; i--)
        {
            if (Surfaces[i].SurfaceId == e.Surface.SurfaceId)
            {
                Surfaces.RemoveAt(i);
                break;
            }
        }
    }

    private void OnComponentsUpdated(object? sender, ComponentsUpdatedEventArgs e) =>
        SurfaceRefreshRequested?.Invoke(this, EventArgs.Empty);

    private void OnDataModelUpdated(object? sender, DataModelUpdatedEventArgs e) =>
        SurfaceRefreshRequested?.Invoke(this, EventArgs.Empty);

    // ── Dispose ───────────────────────────────────────────

    public void Dispose()
    {
        _manager.SurfaceCreated -= OnSurfaceCreated;
        _manager.SurfaceDeleted -= OnSurfaceDeleted;
        _manager.ComponentsUpdated -= OnComponentsUpdated;
        _manager.DataModelUpdated -= OnDataModelUpdated;
    }
}
