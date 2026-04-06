using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace A2Ui.Avalonia.Catalog;

/// <summary>A2UI "Column" → vertical layout panel.</summary>
public sealed class ColumnCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Column";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var children = ctx.RenderChildren(c.Id).ToList();
        return LayoutHelper.BuildLayout(Orientation.Vertical, children, c);
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "Row" → horizontal layout panel.</summary>
public sealed class RowCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Row";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var children = ctx.RenderChildren(c.Id).ToList();
        return LayoutHelper.BuildLayout(Orientation.Horizontal, children, c);
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
    /// <summary>
    /// Build the appropriate layout panel for the given orientation and justify mode.
    /// Uses Grid for spaceBetween (distributes children with star-sized spacers),
    /// StackPanel for all other modes.
    /// </summary>
    public static Control BuildLayout(Orientation orientation, List<Control> children,
                                      A2UiComponent c)
    {
        if (c.Justify is "spaceBetween")
            return BuildSpaceBetweenGrid(orientation, children, c);

        var panel = new StackPanel { Orientation = orientation, Spacing = 8 };
        foreach (var child in children)
            panel.Children.Add(child);

        // justify maps to main-axis self-alignment (packs the group start/center/end)
        if (orientation == Orientation.Vertical)
        {
            panel.VerticalAlignment = MapMainAxis(c.Justify);
            panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        else
        {
            panel.HorizontalAlignment = MapMainAxisH(c.Justify);
            panel.VerticalAlignment = VerticalAlignment.Stretch;
        }

        // align maps to cross-axis alignment on each child
        ApplyCrossAxisAlignment(children, orientation, c.Align);

        return panel;
    }

    /// <summary>
    /// Build a Grid that distributes children with star-sized spacers between them,
    /// emulating CSS justify-content: space-between.
    /// Layout: [Auto] [*] [Auto] [*] [Auto]  (for 3 children)
    /// </summary>
    private static Grid BuildSpaceBetweenGrid(Orientation orientation,
                                               List<Control> children,
                                               A2UiComponent c)
    {
        var grid = new Grid();

        if (children.Count == 0)
            return grid;

        bool isHorizontal = orientation == Orientation.Horizontal;

        for (int i = 0; i < children.Count; i++)
        {
            // Auto column/row for the child
            if (isHorizontal)
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            else
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var child = children[i];
            if (isHorizontal)
                Grid.SetColumn(child, i * 2);
            else
                Grid.SetRow(child, i * 2);
            grid.Children.Add(child);

            // Star spacer between children (not after last)
            if (i < children.Count - 1)
            {
                if (isHorizontal)
                    grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
                else
                    grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
            }
        }

        // align maps to cross-axis alignment on each child
        ApplyCrossAxisAlignment(children, orientation, c.Align);

        return grid;
    }

    /// <summary>
    /// Apply cross-axis alignment to each child control.
    /// For a horizontal row, cross-axis is vertical; for a vertical column, cross-axis is horizontal.
    /// </summary>
    private static void ApplyCrossAxisAlignment(List<Control> children,
                                                 Orientation orientation, string? align)
    {
        if (align is null)
            return;

        foreach (var child in children)
        {
            if (orientation == Orientation.Horizontal)
                child.VerticalAlignment = MapCrossAxisV(align);
            else
                child.HorizontalAlignment = MapCrossAxisH(align);
        }
    }

    private static VerticalAlignment MapMainAxis(string? justify) => justify switch
    {
        "start"   => VerticalAlignment.Top,
        "center"  => VerticalAlignment.Center,
        "end"     => VerticalAlignment.Bottom,
        _         => VerticalAlignment.Stretch,
    };

    private static HorizontalAlignment MapMainAxisH(string? justify) => justify switch
    {
        "start"   => HorizontalAlignment.Left,
        "center"  => HorizontalAlignment.Center,
        "end"     => HorizontalAlignment.Right,
        _         => HorizontalAlignment.Stretch,
    };

    private static VerticalAlignment MapCrossAxisV(string? align) => align switch
    {
        "start"   => VerticalAlignment.Top,
        "center"  => VerticalAlignment.Center,
        "end"     => VerticalAlignment.Bottom,
        "stretch" => VerticalAlignment.Stretch,
        _         => VerticalAlignment.Stretch,
    };

    private static HorizontalAlignment MapCrossAxisH(string? align) => align switch
    {
        "start"   => HorizontalAlignment.Left,
        "center"  => HorizontalAlignment.Center,
        "end"     => HorizontalAlignment.Right,
        "stretch" => HorizontalAlignment.Stretch,
        _         => HorizontalAlignment.Stretch,
    };
}
