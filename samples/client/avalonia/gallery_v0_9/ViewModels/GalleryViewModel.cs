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

    public GalleryViewModel(SurfaceManager manager)
    {
        _manager = manager;

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

    public event EventHandler? SurfaceRefreshRequested;

    // ── Initialization ────────────────────────────────────

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        try
        {
            IReadOnlyList<DemoItem> items = await GalleryDataLoader.LoadAsync(ct).ConfigureAwait(true);
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
