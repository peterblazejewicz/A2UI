using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;

namespace A2Ui.Rendering.Catalog;

/// <summary>A2UI "List" → ScrollViewer + StackPanel (scrollable list).</summary>
public sealed class ListCatalogEntry : ICatalogEntry
{
    public string ComponentType => "List";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var orientation = c.Direction == "horizontal"
            ? Avalonia.Layout.Orientation.Horizontal
            : Avalonia.Layout.Orientation.Vertical;

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
/// A2UI "Modal" → Panel rendering trigger and content.
/// Full popup/dialog behavior is future work; for now renders both inline.
/// </summary>
public sealed class ModalCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Modal";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var panel = new StackPanel { Spacing = 8 };

        if (c.Trigger is not null)
        {
            var trigger = ctx.RenderChild(c.Trigger);
            if (trigger is not null) panel.Children.Add(trigger);
        }

        if (c.Content is not null)
        {
            var content = ctx.RenderChild(c.Content);
            if (content is not null) panel.Children.Add(content);
        }

        return panel;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}
