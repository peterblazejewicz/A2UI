using A2Ui.Avalonia.Catalog;
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

    public A2UiSurface() : this(CatalogRegistry.CreateDefault()) { }

    public A2UiSurface(CatalogRegistry catalog)
    {
        _renderer = new A2UiRenderer(catalog);
        _renderer.UserActionFired += OnUserActionFired;
    }

    public Surface? Surface
    {
        get => GetValue(SurfaceProperty);
        set => SetValue(SurfaceProperty, value);
    }

    public event EventHandler<UserActionEventArgs>? UserActionFired;

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
}
