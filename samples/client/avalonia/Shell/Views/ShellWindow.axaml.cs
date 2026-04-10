using A2Ui.Avalonia;
using A2Ui.Avalonia.Controls;
using A2Ui.Avalonia.Shell.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2Ui.Avalonia.Shell.Views;

public partial class ShellWindow : Window
{
    private readonly ILogger<ShellWindow> _logger;
    private ShellViewModel? _vm;
    private ILoggerFactory? _loggerFactory;

    // Parameterless constructor required by Avalonia XAML loader (AVLN3001).
    public ShellWindow()
        : this(NullLogger<ShellWindow>.Instance) { }

    public ShellWindow(ILogger<ShellWindow> logger)
    {
        _logger = logger;
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        UnwireViewModel();

        if (DataContext is ShellViewModel vm)
        {
            _vm = vm;

            // Capture logger factory before wiring events so it's available immediately
            var loggerFactory = App.Services.GetService<ILoggerFactory>();
            if (loggerFactory is not null)
                _loggerFactory = loggerFactory;

            vm.SurfaceRefreshRequested += OnSurfaceRefreshRequested;
        }
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is ShellViewModel vm)
        {
            try
            {
                await vm.InitializeAsync().ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
                // Window closed during initialization
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize shell");
            }
        }

        // Wire Enter key on prompt input
        var promptInput = this.FindControl<TextBox>("PromptInput");
        if (promptInput is not null)
        {
            promptInput.KeyDown += OnPromptKeyDown;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        UnwireViewModel();
        (_vm as IDisposable)?.Dispose();
        _vm = null;
        base.OnClosed(e);
    }

    private void OnPromptKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _vm?.SendCommand.CanExecute(null) == true)
        {
            _vm.SendCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnSurfaceRefreshRequested(object? sender, EventArgs e)
    {
        // Wire event handlers on any newly created surfaces before refreshing
        WireSurfaceEvents();

        // Refresh all A2UiSurface controls in the visual tree
        foreach (A2UiSurface surface in this.GetVisualDescendants().OfType<A2UiSurface>())
            surface.Refresh();
    }

    /// <summary>
    /// Subscribes to <see cref="A2UiSurface.UserActionFired"/> and
    /// <see cref="A2UiSurface.DataModelChanged"/> on each A2UiSurface in the visual tree.
    /// Also injects the logger factory once per surface. The ItemsControl creates
    /// A2UiSurface controls dynamically, so we scan the visual tree after each refresh.
    /// </summary>
    private void WireSurfaceEvents()
    {
        foreach (A2UiSurface surface in this.GetVisualDescendants().OfType<A2UiSurface>())
        {
            // Avoid double-subscribing by tagging wired surfaces
            if (surface.Tag is "__wired")
                continue;

            surface.Tag = "__wired";
            surface.UserActionFired += OnUserActionFired;
            surface.DataModelChanged += OnDataModelChanged;

            // Inject logger factory once per surface (not on every refresh)
            if (_loggerFactory is not null)
                surface.SetLoggerFactory(_loggerFactory);
        }
    }

    private async void OnUserActionFired(object? sender, UserActionEventArgs e)
    {
        if (_vm is null)
            return;

        try
        {
            await _vm.HandleUserActionAsync(e).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling user action {Action}", e.EventName);
        }
    }

    private void OnDataModelChanged(object? sender, DataModelChangedEventArgs e)
    {
        // Re-render surfaces so that check validations, data-bound text,
        // and button enabled state update after two-way binding changes.
        if (sender is A2UiSurface surface)
            surface.Refresh();
    }

    private void UnwireViewModel()
    {
        if (_vm is not null)
            _vm.SurfaceRefreshRequested -= OnSurfaceRefreshRequested;

        // Unwire surface event handlers
        foreach (A2UiSurface surface in this.GetVisualDescendants().OfType<A2UiSurface>())
        {
            surface.UserActionFired -= OnUserActionFired;
            surface.DataModelChanged -= OnDataModelChanged;
            surface.Tag = null;
        }
    }
}
