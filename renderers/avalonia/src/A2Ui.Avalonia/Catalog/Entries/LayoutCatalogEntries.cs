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
        double[]? weights = LayoutHelper.CollectWeights(c, ctx);
        return LayoutHelper.BuildLayout(Orientation.Vertical, children, c, weights);
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm, IRenderContext ctx) => false;
}

/// <summary>A2UI "Row" → horizontal layout panel.</summary>
public sealed class RowCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Row";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var children = ctx.RenderChildren(c.Id).ToList();
        double[]? weights = LayoutHelper.CollectWeights(c, ctx);
        return LayoutHelper.BuildLayout(Orientation.Horizontal, children, c, weights);
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm, IRenderContext ctx) => false;
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

    public bool Update(Control existing, A2UiComponent c, DataModel dm, IRenderContext ctx) => false;
}

internal static class LayoutHelper
{
    /// <summary>
    /// Collect the weight values for all children of the given component.
    /// Returns null when no child has a weight > 0 (falls back to normal layout).
    /// </summary>
    public static double[]? CollectWeights(A2UiComponent c, IRenderContext ctx)
    {
        if (c.Children?.Ids is not { } ids || ids.Length == 0)
            return null;

        double[] weights = ids.Select(id => ctx.GetComponentWeight(id) ?? 0).ToArray();
        return Array.Exists(weights, w => w > 0) ? weights : null;
    }

    /// <summary>
    /// Build the appropriate layout panel for the given orientation and justify mode.
    /// When weights are provided and any weight > 0, uses a proportional star-sized Grid.
    /// Uses Grid for spaceBetween (distributes children with star-sized spacers),
    /// StackPanel for all other modes.
    /// </summary>
    public static Control BuildLayout(
        Orientation orientation,
        List<Control> children,
        A2UiComponent c,
        double[]? weights = null
    )
    {
        if (weights is not null && weights.Any(w => w > 0))
            return BuildWeightedGrid(orientation, children, c, weights);

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
    /// Build a Grid with proportional star-sized columns/rows based on child weights.
    /// Children with weight > 0 get Star(weight) sizing; weight == 0 gets Auto sizing.
    /// </summary>
    private static Grid BuildWeightedGrid(
        Orientation orientation,
        List<Control> children,
        A2UiComponent c,
        double[] weights
    )
    {
        var grid = new Grid();

        if (children.Count == 0)
            return grid;

        bool isHorizontal = orientation == Orientation.Horizontal;

        for (int i = 0; i < children.Count; i++)
        {
            double weight = i < weights.Length ? weights[i] : 0;
            var length = weight > 0 ? new GridLength(weight, GridUnitType.Star) : GridLength.Auto;

            if (isHorizontal)
                grid.ColumnDefinitions.Add(new ColumnDefinition(length));
            else
                grid.RowDefinitions.Add(new RowDefinition(length));

            var child = children[i];
            if (isHorizontal)
                Grid.SetColumn(child, i);
            else
                Grid.SetRow(child, i);
            grid.Children.Add(child);
        }

        // apply cross-axis alignment to children
        ApplyCrossAxisAlignment(children, orientation, c.Align);

        return grid;
    }

    /// <summary>
    /// Build a Grid that distributes children with star-sized spacers between them,
    /// emulating CSS justify-content: space-between.
    /// Layout: [Auto] [*] [Auto] [*] [Auto]  (for 3 children)
    /// </summary>
    private static Grid BuildSpaceBetweenGrid(Orientation orientation, List<Control> children, A2UiComponent c)
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
    private static void ApplyCrossAxisAlignment(List<Control> children, Orientation orientation, string? align)
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

    private static VerticalAlignment MapMainAxis(string? justify) =>
        justify switch
        {
            "start" => VerticalAlignment.Top,
            "center" => VerticalAlignment.Center,
            "end" => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Stretch,
        };

    private static HorizontalAlignment MapMainAxisH(string? justify) =>
        justify switch
        {
            "start" => HorizontalAlignment.Left,
            "center" => HorizontalAlignment.Center,
            "end" => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Stretch,
        };

    private static VerticalAlignment MapCrossAxisV(string? align) =>
        align switch
        {
            "start" => VerticalAlignment.Top,
            "center" => VerticalAlignment.Center,
            "end" => VerticalAlignment.Bottom,
            "stretch" => VerticalAlignment.Stretch,
            _ => VerticalAlignment.Stretch,
        };

    private static HorizontalAlignment MapCrossAxisH(string? align) =>
        align switch
        {
            "start" => HorizontalAlignment.Left,
            "center" => HorizontalAlignment.Center,
            "end" => HorizontalAlignment.Right,
            "stretch" => HorizontalAlignment.Stretch,
            _ => HorizontalAlignment.Stretch,
        };
}
