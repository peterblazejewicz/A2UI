using A2Ui.Avalonia;
using A2Ui.Avalonia.Controls;
using A2Ui.Avalonia.Gallery.ViewModels;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2Ui.Avalonia.Gallery.Views;

public partial class GalleryWindow : Window
{
    private readonly ILogger<GalleryWindow> _logger;
    private GalleryViewModel? _vm;
    private A2UiSurface? _surfaceHost;

    // Parameterless constructor required by Avalonia XAML loader (AVLN3001).
    public GalleryWindow()
        : this(NullLogger<GalleryWindow>.Instance) { }

    public GalleryWindow(ILogger<GalleryWindow> logger)
    {
        _logger = logger;
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Unsubscribe from previous ViewModel if DataContext changes
        UnwireViewModel();

        if (DataContext is GalleryViewModel vm)
        {
            _vm = vm;
            _surfaceHost = this.FindControl<A2UiSurface>("SurfaceHost");

            if (_surfaceHost is null)
            {
                _logger.LogWarning("SurfaceHost control not found in visual tree");
                return;
            }

            // Wire logging into the XAML-constructed A2UiSurface so that
            // renderer, function registry, and image loading all log via Serilog.
            var loggerFactory = App.Services.GetService<ILoggerFactory>();
            if (loggerFactory is not null)
                _surfaceHost.SetLoggerFactory(loggerFactory);

            vm.SurfaceRefreshRequested += OnSurfaceRefreshRequested;
            _surfaceHost.UserActionFired += OnUserActionFired;
            _surfaceHost.DataModelChanged += OnDataModelChanged;
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
            catch (OperationCanceledException)
            {
                // Window closed during initialization — nothing to do
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize gallery");
                Title = $"A2UI Gallery — Error: {ex.Message}";
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        UnwireViewModel();
        (_vm as IDisposable)?.Dispose();
        _vm = null;
        base.OnClosed(e);
    }

    private void OnSurfaceRefreshRequested(object? sender, EventArgs e) => _surfaceHost?.Refresh();

    private void OnUserActionFired(object? sender, UserActionEventArgs args) => _vm?.LogAction(args);

    private void OnDataModelChanged(object? sender, DataModelChangedEventArgs e)
    {
        _vm?.RefreshDataModelJson();
        // Re-render the surface so that check validations, data-bound text,
        // and button enabled state update after two-way binding changes.
        _surfaceHost?.Refresh();
    }

    private void UnwireViewModel()
    {
        if (_vm is not null)
            _vm.SurfaceRefreshRequested -= OnSurfaceRefreshRequested;

        if (_surfaceHost is not null)
        {
            _surfaceHost.UserActionFired -= OnUserActionFired;
            _surfaceHost.DataModelChanged -= OnDataModelChanged;
        }
    }
}
