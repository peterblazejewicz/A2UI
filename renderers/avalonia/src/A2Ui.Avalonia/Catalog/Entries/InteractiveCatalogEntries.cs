using A2Ui.Core.Components;
using A2Ui.Core.Surfaces;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace A2Ui.Avalonia.Catalog.Entries;

/// <summary>A2UI "List" → ScrollViewer + StackPanel (scrollable list).</summary>
public sealed class ListCatalogEntry : ICatalogEntry
{
    /// <inheritdoc />
    public string ComponentType => "List";

    /// <inheritdoc />
    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (ListComponent)component;
        var orientation = typed.Direction == "horizontal" ? Orientation.Horizontal : Orientation.Vertical;

        var panel = new StackPanel { Orientation = orientation, Spacing = 4 };
        foreach (var child in context.RenderChildren(component.Id))
        {
            panel.Children.Add(child);
        }

        return new ScrollViewer { Content = panel };
    }

    /// <inheritdoc />
    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (ListComponent)component;
        if (existing is not ScrollViewer sv || sv.Content is not StackPanel panel)
        {
            return false;
        }

        var newOrientation = typed.Direction == "horizontal" ? Orientation.Horizontal : Orientation.Vertical;
        if (panel.Orientation != newOrientation)
        {
            return false;
        }

        panel.Children.Clear();
        foreach (var child in context.RenderChildren(component.Id))
        {
            panel.Children.Add(child);
        }

        return true;
    }
}

/// <summary>A2UI "Tabs" → TabControl.</summary>
public sealed class TabsCatalogEntry : ICatalogEntry
{
    /// <inheritdoc />
    public string ComponentType => "Tabs";

    /// <inheritdoc />
    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (TabsComponent)component;
        var tc = new TabControl();

        if (typed.Tabs is { } tabs)
        {
            foreach (var tab in tabs)
            {
                tc.Items.Add(
                    new TabItem { Header = context.Resolve(tab.Title), Content = context.RenderChild(tab.Child) }
                );
            }
        }

        return tc;
    }

    /// <inheritdoc />
    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (TabsComponent)component;
        if (existing is not TabControl tc)
        {
            return false;
        }

        if (typed.Tabs is not { } tabs || tabs.Length != tc.Items.Count)
        {
            return false;
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tc.Items[i] is not TabItem tabItem)
            {
                return false;
            }

            tabItem.Header = context.Resolve(tabs[i].Title);
            tabItem.Content = context.RenderChild(tabs[i].Child);
        }

        return true;
    }
}

/// <summary>
/// A2UI "Modal" → Panel with trigger control + Avalonia Popup overlay.
/// Clicking the trigger opens a centered popup; close button or light-dismiss closes it.
/// </summary>
public sealed class ModalCatalogEntry : ICatalogEntry
{
    /// <inheritdoc />
    public string ComponentType => "Modal";

    /// <inheritdoc />
    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (ModalComponent)component;
        var container = new Panel();

        // Render the trigger control (shown permanently)
        Control? triggerControl = null;
        if (typed.Trigger is not null)
        {
            triggerControl = context.RenderChild(typed.Trigger);
            if (triggerControl is not null)
            {
                container.Children.Add(triggerControl);
            }
        }

        // Build the Popup overlay for the content
        if (typed.Content is not null)
        {
            var popup = new Popup
            {
                IsLightDismissEnabled = true,
                Placement = PlacementMode.Center,
                PlacementTarget = triggerControl ?? container,
            };

            // Close button positioned at the top-right of the popup content
            var closeBtn = new Button { Content = "×", HorizontalAlignment = HorizontalAlignment.Right };
            closeBtn.Classes.Add("ModalClose");

            // Stack: close button above the rendered content
            var contentColumn = new StackPanel { Spacing = 8 };
            contentColumn.Children.Add(closeBtn);

            Control? renderedContent = context.RenderChild(typed.Content);
            if (renderedContent is not null)
            {
                contentColumn.Children.Add(renderedContent);
            }

            // White rounded card wrapping the content
            popup.Child = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                MinWidth = 300,
                MaxWidth = 600,
                Child = contentColumn,
            };

            // Wire trigger → open, close button → close
            triggerControl?.Tapped += (_, _) => popup.IsOpen = true;

            closeBtn.Click += (_, _) => popup.IsOpen = false;

            // Center popup on the top-level window once attached to visual tree
            container.AttachedToVisualTree += (_, _) =>
            {
                var topLevel = TopLevel.GetTopLevel(container);
                if (topLevel is Control topControl)
                {
                    popup.PlacementTarget = topControl;
                }
            };

            // Clean up popup on detach to prevent event handler leaks
            container.DetachedFromVisualTree += (_, _) =>
            {
                popup.IsOpen = false;
                popup.Child = null;
            };

            container.Children.Add(popup);
        }

        return container;
    }

    /// <inheritdoc />
    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context) => false; // Popup.PlacementTarget goes stale if trigger is recreated; lifecycle handlers are not idempotent
}
