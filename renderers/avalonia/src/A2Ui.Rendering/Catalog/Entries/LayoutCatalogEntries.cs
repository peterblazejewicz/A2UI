using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Layout;

namespace A2Ui.Rendering.Catalog;

/// <summary>A2UI "Column" → StackPanel (Vertical).</summary>
public sealed class ColumnCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Column";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        foreach (var child in ctx.RenderChildren(c.Id))
            panel.Children.Add(child);
        return panel;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "Row" → StackPanel (Horizontal) with wrap.</summary>
public sealed class RowCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Row";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var panel = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var child in ctx.RenderChildren(c.Id))
            panel.Children.Add(child);
        return panel;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "Card" → Border with shadow effect.</summary>
public sealed class CardCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Card";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var border = new Border
        {
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(16),
            Classes = { "Card" },
        };
        var panel = new StackPanel { Spacing = 8 };
        foreach (var child in ctx.RenderChildren(c.Id))
            panel.Children.Add(child);
        border.Child = panel;
        return border;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}