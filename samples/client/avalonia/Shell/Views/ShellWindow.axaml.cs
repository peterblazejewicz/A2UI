using A2Ui.Avalonia.Controls;
using A2Ui.Avalonia.Shell.ViewModels;
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
        this._logger = logger;
        this.InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        this.UnwireViewModel();

        if (this.DataContext is ShellViewModel vm)
        {
            this._vm = vm;

            // Capture logger factory before wiring events so it's available immediately
            var loggerFactory = App.Services.GetService<ILoggerFactory>();
            if (loggerFactory is not null)
            {
                this._loggerFactory = loggerFactory;
            }

            vm.SurfaceRefreshRequested += this.OnSurfaceRefreshRequested;
        }
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (this.DataContext is ShellViewModel vm)
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
                this._logger.LogError(ex, "Failed to initialize shell");
            }
        }

        // Wire Enter key on prompt input
        var promptInput = this.FindControl<TextBox>("PromptInput");
        if (promptInput is not null)
        {
            promptInput.KeyDown += this.OnPromptKeyDown;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        this.UnwireViewModel();
        (this._vm as IDisposable)?.Dispose();
        this._vm = null;
        base.OnClosed(e);
    }

    private void OnPromptKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && this._vm?.SendCommand.CanExecute(null) == true)
        {
            this._vm.SendCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnSurfaceRefreshRequested(object? sender, EventArgs e)
    {
        // Wire event handlers on any newly created surfaces before refreshing
        this.WireSurfaceEvents();

        // Refresh all A2UiSurface controls in the visual tree
        foreach (A2UiSurface surface in this.GetVisualDescendants().OfType<A2UiSurface>())
        {
            surface.Refresh();
        }
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
            {
                continue;
            }

            surface.Tag = "__wired";
            surface.UserActionFired += this.OnUserActionFiredAsync;
            surface.DataModelChanged += this.OnDataModelChanged;

            // Inject logger factory once per surface (not on every refresh)
            if (this._loggerFactory is not null)
            {
                surface.SetLoggerFactory(this._loggerFactory);
            }
        }
    }

    private async void OnUserActionFiredAsync(object? sender, UserActionEventArgs e)
    {
        if (this._vm is null)
        {
            return;
        }

        try
        {
            await this._vm.HandleUserActionAsync(e).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error handling user action {Action}", e.EventName);
        }
    }

    private void OnDataModelChanged(object? sender, DataModelChangedEventArgs e)
    {
        // Re-render surfaces so that check validations, data-bound text,
        // and button enabled state update after two-way binding changes.
        if (sender is A2UiSurface surface)
        {
            surface.Refresh();
        }
    }

    private void UnwireViewModel()
    {
        if (this._vm is not null)
        {
            this._vm.SurfaceRefreshRequested -= this.OnSurfaceRefreshRequested;
        }

        // Unwire surface event handlers
        foreach (A2UiSurface surface in this.GetVisualDescendants().OfType<A2UiSurface>())
        {
            surface.UserActionFired -= this.OnUserActionFiredAsync;
            surface.DataModelChanged -= this.OnDataModelChanged;
            surface.Tag = null;
        }
    }
}
