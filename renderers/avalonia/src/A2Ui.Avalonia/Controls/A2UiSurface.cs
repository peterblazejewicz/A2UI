using A2Ui.Avalonia.Catalog;
using A2Ui.Avalonia.Functions;
using A2Ui.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace A2Ui.Avalonia.Controls;

/// <summary>
/// Avalonia host control for an A2UI surface.
/// Set <see cref="Surface"/> to render initially, then call <see cref="Refresh"/>
/// when the surface's components are updated (same reference, mutated in place).
/// </summary>
public sealed class A2UiSurface : ContentControl
{
    public static readonly StyledProperty<Surface?> SurfaceProperty =
        AvaloniaProperty.Register<A2UiSurface, Surface?>(nameof(Surface));

    private readonly A2UiRenderer _renderer;

    public A2UiSurface() : this(CatalogRegistry.CreateDefault(), FunctionRegistry.CreateDefault()) { }

    public A2UiSurface(CatalogRegistry catalog, IFunctionRegistry? functionRegistry = null)
    {
        _renderer = new A2UiRenderer(catalog, functionRegistry);
        _renderer.UserActionFired += OnUserActionFired;
        _renderer.DataModelChanged += OnDataModelChanged;

        // Load default component styles (typography, card, button variants).
        // Consuming apps override these via Application-level styles.
        Styles.Add(new global::Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://A2Ui.Avalonia"))
        {
            Source = new Uri("avares://A2Ui.Avalonia/Themes/A2UiDefaultStyles.axaml"),
        });
    }

    public Surface? Surface
    {
        get => GetValue(SurfaceProperty);
        set => SetValue(SurfaceProperty, value);
    }

    public event EventHandler<UserActionEventArgs>? UserActionFired;
    public event EventHandler<DataModelChangedEventArgs>? DataModelChanged;

    /// <summary>
    /// Re-render the current surface. Call this after SurfaceManager fires
    /// ComponentsUpdated or DataModelUpdated — the Surface reference doesn't
    /// change (it's mutated in place), so Avalonia property change won't fire.
    /// Do NOT call this on the initial Surface assignment — OnPropertyChanged handles that.
    /// </summary>
    public void Refresh()
    {
        var surface = Surface;
        if (surface is null)
            return;

        if (Dispatcher.UIThread.CheckAccess())
            Content = _renderer.Render(surface);
        else
            Dispatcher.UIThread.Post(() => Content = _renderer.Render(surface));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SurfaceProperty)
        {
            // Clear the renderer's control cache for the old surface so that
            // stale controls (with closures over the old Surface/DataModel)
            // are not reused when a new surface is created with the same ID.
            var oldSurface = change.GetOldValue<Surface?>();
            if (oldSurface is not null)
                _renderer.ClearSurface(oldSurface.SurfaceId);

            var surface = change.GetNewValue<Surface?>();
            if (Dispatcher.UIThread.CheckAccess())
            {
                Content = surface is null ? null : _renderer.Render(surface);
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                    Content = surface is null ? null : _renderer.Render(surface));
            }
        }
    }

    private void OnUserActionFired(object? sender, UserActionEventArgs e) =>
        UserActionFired?.Invoke(this, e);

    private void OnDataModelChanged(object? sender, DataModelChangedEventArgs e) =>
        DataModelChanged?.Invoke(this, e);
}
