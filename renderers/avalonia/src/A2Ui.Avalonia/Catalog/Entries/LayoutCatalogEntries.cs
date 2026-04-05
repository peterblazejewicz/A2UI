using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace A2Ui.Avalonia.Catalog;

/// <summary>A2UI "Column" → StackPanel (Vertical).</summary>
public sealed class ColumnCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Column";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, Spacing = 8 };
        LayoutHelper.ApplyAlignment(panel, c);
        foreach (var child in ctx.RenderChildren(c.Id))
            panel.Children.Add(child);
        return panel;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "Row" → StackPanel (Horizontal).</summary>
public sealed class RowCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Row";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        LayoutHelper.ApplyAlignment(panel, c);
        foreach (var child in ctx.RenderChildren(c.Id))
            panel.Children.Add(child);
        return panel;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "Card" → Border with rounded corners and padding.</summary>
public sealed class CardCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Card";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Classes = { "Card" },
        };

        // Card uses single child per v0.9 spec
        if (c.Child is not null)
        {
            border.Child = ctx.RenderChild(c.Child);
        }
        else
        {
            // Fallback: render children for backward compat
            var panel = new StackPanel { Spacing = 8 };
            foreach (var child in ctx.RenderChildren(c.Id))
                panel.Children.Add(child);
            border.Child = panel;
        }

        return border;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

internal static class LayoutHelper
{
    public static void ApplyAlignment(StackPanel panel, A2UiComponent c)
    {
        if (panel.Orientation == Orientation.Vertical)
        {
            panel.VerticalAlignment = MapVertical(c.Justify);
            panel.HorizontalAlignment = MapHorizontal(c.Align);
        }
        else
        {
            panel.HorizontalAlignment = MapHorizontal(c.Justify);
            panel.VerticalAlignment = MapVertical(c.Align);
        }
    }

    private static HorizontalAlignment MapHorizontal(string? value) => value switch
    {
        "start"   => HorizontalAlignment.Left,
        "center"  => HorizontalAlignment.Center,
        "end"     => HorizontalAlignment.Right,
        "stretch" => HorizontalAlignment.Stretch,
        _         => HorizontalAlignment.Stretch,
    };

    private static VerticalAlignment MapVertical(string? value) => value switch
    {
        "start"   => VerticalAlignment.Top,
        "center"  => VerticalAlignment.Center,
        "end"     => VerticalAlignment.Bottom,
        "stretch" => VerticalAlignment.Stretch,
        _         => VerticalAlignment.Stretch,
    };
}
