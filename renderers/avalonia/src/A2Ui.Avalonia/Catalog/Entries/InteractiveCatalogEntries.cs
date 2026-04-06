using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace A2Ui.Avalonia.Catalog;

/// <summary>A2UI "List" → ScrollViewer + StackPanel (scrollable list).</summary>
public sealed class ListCatalogEntry : ICatalogEntry
{
    public string ComponentType => "List";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var orientation = c.Direction == "horizontal"
            ? Orientation.Horizontal
            : Orientation.Vertical;

        var panel = new StackPanel { Orientation = orientation, Spacing = 4 };
        foreach (var child in ctx.RenderChildren(c.Id))
            panel.Children.Add(child);

        return new ScrollViewer { Content = panel };
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "Tabs" → TabControl.</summary>
public sealed class TabsCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Tabs";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var tc = new TabControl();

        if (c.Tabs is { } tabs)
        {
            foreach (var tab in tabs)
            {
                tc.Items.Add(new TabItem
                {
                    Header  = tab.Title,
                    Content = ctx.RenderChild(tab.Child),
                });
            }
        }

        return tc;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>
/// A2UI "Modal" → Panel with trigger control + Avalonia Popup overlay.
/// Clicking the trigger opens a centered popup; close button or light-dismiss closes it.
/// </summary>
public sealed class ModalCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Modal";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var container = new Panel();

        // Render the trigger control (shown permanently)
        Control? triggerControl = null;
        if (c.Trigger is not null)
        {
            triggerControl = ctx.RenderChild(c.Trigger);
            if (triggerControl is not null)
                container.Children.Add(triggerControl);
        }

        // Build the Popup overlay for the content
        if (c.Content is not null)
        {
            var popup = new Popup
            {
                IsLightDismissEnabled = true,
                Placement = PlacementMode.Center,
                PlacementTarget = triggerControl ?? container,
            };

            // Close button positioned at the top-right of the popup content
            var closeBtn = new Button
            {
                Content = "×",
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            closeBtn.Classes.Add("ModalClose");

            // Stack: close button above the rendered content
            var contentColumn = new StackPanel { Spacing = 8 };
            contentColumn.Children.Add(closeBtn);

            Control? renderedContent = ctx.RenderChild(c.Content);
            if (renderedContent is not null)
                contentColumn.Children.Add(renderedContent);

            // White rounded card wrapping the content
            var contentPanel = new Border
            {
                Background = new SolidColorBrush(Colors.White),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                MinWidth = 300,
                MaxWidth = 600,
                Child = contentColumn,
            };

            popup.Child = contentPanel;

            // Wire trigger → open, close button → close
            if (triggerControl is not null)
                triggerControl.Tapped += (_, _) => popup.IsOpen = true;

            closeBtn.Click += (_, _) => popup.IsOpen = false;

            // Center popup on the top-level window once attached to visual tree
            container.AttachedToVisualTree += (_, _) =>
            {
                var topLevel = TopLevel.GetTopLevel(container);
                if (topLevel is Control topControl)
                    popup.PlacementTarget = topControl;
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

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}
