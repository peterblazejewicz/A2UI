using A2Ui.Avalonia.Catalog;
using A2Ui.Avalonia.Functions;
using A2Ui.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace A2Ui.Avalonia.Controls;

/// <summary>
/// Avalonia host control for an A2UI surface.
/// Set <see cref="Surface"/> to render initially, then call <see cref="Refresh"/>
/// when the surface's components are updated (same reference, mutated in place).
/// </summary>
public sealed class A2UiSurface : ContentControl
{
    /// <summary>Identifies the <see cref="Surface"/> styled property.</summary>
    public static readonly StyledProperty<Surface?> SurfaceProperty = AvaloniaProperty.Register<A2UiSurface, Surface?>(
        nameof(Surface)
    );

    private A2UiRenderer _renderer;
    private bool _isRendering;
    private bool _renderPending;

    /// <summary>Initializes a new instance with default catalog and function registries.</summary>
    public A2UiSurface()
        : this(CatalogRegistry.CreateDefault(), FunctionRegistry.CreateDefault()) { }

    /// <summary>
    /// Initializes a new instance with the specified catalog and function registries.
    /// </summary>
    /// <param name="catalog">Registry mapping component types to Avalonia control factories.</param>
    /// <param name="functionRegistry">Optional function registry for DynamicValue evaluation.</param>
    /// <param name="loggerFactory">Optional logger factory for diagnostics.</param>
    public A2UiSurface(
        CatalogRegistry catalog,
        IFunctionRegistry? functionRegistry = null,
        ILoggerFactory? loggerFactory = null
    )
    {
        this._renderer = new A2UiRenderer(catalog, functionRegistry, loggerFactory);
        this._renderer.UserActionFired += this.OnUserActionFired;
        this._renderer.DataModelChanged += this.OnDataModelChanged;

        // Load default component styles (typography, card, button variants).
        // Consuming apps override these via Application-level styles.
        this.Styles.Add(
            new global::Avalonia.Markup.Xaml.Styling.StyleInclude(new Uri("avares://A2Ui.Avalonia"))
            {
                Source = new Uri("avares://A2Ui.Avalonia/Themes/A2UiDefaultStyles.axaml"),
            }
        );
    }

    /// <summary>Gets or sets the A2UI surface to render.</summary>
    public Surface? Surface
    {
        get => this.GetValue(SurfaceProperty);
        set => this.SetValue(SurfaceProperty, value);
    }

    /// <summary>Raised when a user interacts with a rendered component (e.g., button click).</summary>
    public event EventHandler<UserActionEventArgs>? UserActionFired;

    /// <summary>Raised when the data model is mutated via two-way binding.</summary>
    public event EventHandler<DataModelChangedEventArgs>? DataModelChanged;

    /// <summary>
    /// Re-create the internal renderer with logging enabled.
    /// Call this from code-behind after the XAML parameterless constructor
    /// to wire logging from the application's DI container.
    /// </summary>
    public void SetLoggerFactory(ILoggerFactory loggerFactory)
    {
        this._renderer.UserActionFired -= this.OnUserActionFired;
        this._renderer.DataModelChanged -= this.OnDataModelChanged;
        this._renderer = new A2UiRenderer(
            CatalogRegistry.CreateDefault(loggerFactory),
            FunctionRegistry.CreateDefault(loggerFactory),
            loggerFactory
        );
        this._renderer.UserActionFired += this.OnUserActionFired;
        this._renderer.DataModelChanged += this.OnDataModelChanged;
    }

    /// <summary>
    /// Re-render the current surface. Call this after SurfaceManager fires
    /// ComponentsUpdated or DataModelUpdated — the Surface reference doesn't
    /// change (it's mutated in place), so Avalonia property change won't fire.
    /// Do NOT call this on the initial Surface assignment — OnPropertyChanged handles that.
    /// </summary>
    public void Refresh()
    {
        var surface = this.Surface;
        if (surface is null)
        {
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            this.RenderAndRestoreFocus(surface);
        }
        else
        {
            Dispatcher.UIThread.Post(() => this.RenderAndRestoreFocus(surface));
        }
    }

    /// <summary>
    /// Re-render the surface and restore keyboard focus to the same control.
    /// Card/Column entries don't support in-place Update, so Render() creates
    /// new parent containers — but leaf controls (TextBox, etc.) are reused via
    /// the renderer cache. Detaching and reattaching them loses Avalonia focus,
    /// so we save and restore it explicitly.
    /// </summary>
    private void RenderAndRestoreFocus(Surface surface)
    {
        // Guard against re-entrant rendering: input control write-back during
        // Create() fires onDataModelChanged synchronously, which can trigger
        // Refresh() while a render is already in progress. Avalonia 12 throws
        // InvalidOperationException when a control is added to a panel while
        // it still has a visual parent from a concurrent render pass.
        if (this._isRendering)
        {
            this._renderPending = true;
            return;
        }

        this._isRendering = true;
        try
        {
            // Save the currently focused element (may be a TextBox inside the surface)
            var topLevel = TopLevel.GetTopLevel(this);
            IInputElement? focused = topLevel?.FocusManager?.GetFocusedElement();

            this.Content = this._renderer.Render(surface);

            // Restore focus: the same control instance is still in the tree
            // (reused by the renderer cache) but lost focus during re-parenting.
            if (focused is InputElement focusable)
            {
                focusable.Focus();
            }
        }
        finally
        {
            this._isRendering = false;
        }

        // If a data model change requested a re-render while we were busy,
        // do a single follow-up render now that the first one is complete.
        if (this._renderPending)
        {
            this._renderPending = false;
            this.RenderAndRestoreFocus(surface);
        }
    }

    /// <inheritdoc />
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
            {
                this._renderer.ClearSurface(oldSurface.SurfaceId);
            }

            var surface = change.GetNewValue<Surface?>();
            if (Dispatcher.UIThread.CheckAccess())
            {
                this.Content = surface is null ? null : this._renderer.Render(surface);
            }
            else
            {
                Dispatcher.UIThread.Post(() => this.Content = surface is null ? null : this._renderer.Render(surface));
            }
        }
    }

    private void OnUserActionFired(object? sender, UserActionEventArgs e) => UserActionFired?.Invoke(this, e);

    private void OnDataModelChanged(object? sender, DataModelChangedEventArgs e) => DataModelChanged?.Invoke(this, e);
}
