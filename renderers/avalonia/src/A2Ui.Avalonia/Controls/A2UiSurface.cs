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
    }

    public Surface? Surface
    {
        get => GetValue(SurfaceProperty);
        set => SetValue(SurfaceProperty, value);
    }

    public event EventHandler<UserActionEventArgs>? UserActionFired;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SurfaceProperty)
        {
            OnSurfaceChanged(change.GetNewValue<Surface?>());
        }
    }

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
