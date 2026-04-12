using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace A2Ui.Avalonia.Catalog;

/// <summary>A2UI "Column" → vertical layout panel.</summary>
public sealed class ColumnCatalogEntry : ICatalogEntry
{
    /// <inheritdoc />
    public string ComponentType => "Column";

    /// <inheritdoc />
    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (ColumnComponent)component;
        var children = context.RenderChildren(component.Id).ToList();
        double[]? weights = LayoutHelper.CollectWeights(component, context);
        return LayoutHelper.BuildLayout(Orientation.Vertical, children, typed.Justify, typed.Align, weights);
    }

    /// <inheritdoc />
    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context) => false; // Panel type (StackPanel vs Grid) is selected at creation based on justify/weights — cannot be mutated in-place
}

/// <summary>A2UI "Row" → horizontal layout panel.</summary>
public sealed class RowCatalogEntry : ICatalogEntry
{
    /// <inheritdoc />
    public string ComponentType => "Row";

    /// <inheritdoc />
    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (RowComponent)component;
        var children = context.RenderChildren(component.Id).ToList();
        double[]? weights = LayoutHelper.CollectWeights(component, context);
        return LayoutHelper.BuildLayout(Orientation.Horizontal, children, typed.Justify, typed.Align, weights);
    }

    /// <inheritdoc />
    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context) => false; // Panel type (StackPanel vs Grid) is selected at creation based on justify/weights — cannot be mutated in-place
}

/// <summary>A2UI "Card" → Border with rounded corners and padding.</summary>
public sealed class CardCatalogEntry : ICatalogEntry
{
    /// <inheritdoc />
    public string ComponentType => "Card";

    /// <inheritdoc />
    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Classes = { "Card" },
        };

        // Card uses single child per v0.9 spec
        if (component.Child is not null)
        {
            border.Child = context.RenderChild(component.Child);
        }
        else
        {
            // Fallback: render children for backward compat
            var panel = new StackPanel { Spacing = 8 };
            foreach (var child in context.RenderChildren(component.Id))
            {
                panel.Children.Add(child);
            }

            border.Child = panel;
        }

        return border;
    }

    /// <inheritdoc />
    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        if (existing is not Border border)
        {
            return false;
        }

        if (component.Child is not null)
        {
            border.Child = context.RenderChild(component.Child);
            return true;
        }

        return false; // Multi-child fallback: child list may have changed structurally
    }
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
        {
            return null;
        }

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
        string? justify,
        string? align,
        double[]? weights = null
    )
    {
        if (weights?.Any(w => w > 0) == true)
        {
            return BuildWeightedGrid(orientation, children, justify, align, weights);
        }

        if (justify is "spaceBetween")
        {
            return BuildSpaceBetweenGrid(orientation, children, align);
        }

        var panel = new StackPanel { Orientation = orientation, Spacing = 8 };
        foreach (var child in children)
        {
            panel.Children.Add(child);
        }

        // justify maps to main-axis self-alignment (packs the group start/center/end)
        if (orientation == Orientation.Vertical)
        {
            panel.VerticalAlignment = MapMainAxis(justify);
            panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        else
        {
            panel.HorizontalAlignment = MapMainAxisH(justify);
            panel.VerticalAlignment = VerticalAlignment.Stretch;
        }

        // align maps to cross-axis alignment on each child
        ApplyCrossAxisAlignment(children, orientation, align);

        return panel;
    }

    /// <summary>
    /// Build a Grid with proportional star-sized columns/rows based on child weights.
    /// Children with weight > 0 get Star(weight) sizing; weight == 0 gets Auto sizing.
    /// </summary>
    private static Grid BuildWeightedGrid(
        Orientation orientation,
        List<Control> children,
        string? justify,
        string? align,
        double[] weights
    )
    {
        var grid = new Grid();

        if (children.Count == 0)
        {
            return grid;
        }

        bool isHorizontal = orientation == Orientation.Horizontal;

        for (int i = 0; i < children.Count; i++)
        {
            double weight = i < weights.Length ? weights[i] : 0;
            var length = weight > 0 ? new GridLength(weight, GridUnitType.Star) : GridLength.Auto;

            if (isHorizontal)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(length));
            }
            else
            {
                grid.RowDefinitions.Add(new RowDefinition(length));
            }

            var child = children[i];
            if (isHorizontal)
            {
                Grid.SetColumn(child, i);
            }
            else
            {
                Grid.SetRow(child, i);
            }

            grid.Children.Add(child);
        }

        // apply cross-axis alignment to children
        ApplyCrossAxisAlignment(children, orientation, align);

        return grid;
    }

    /// <summary>
    /// Build a Grid that distributes children with star-sized spacers between them,
    /// emulating CSS justify-content: space-between.
    /// Layout: [Auto] [*] [Auto] [*] [Auto]  (for 3 children)
    /// </summary>
    private static Grid BuildSpaceBetweenGrid(Orientation orientation, List<Control> children, string? align)
    {
        var grid = new Grid();

        if (children.Count == 0)
        {
            return grid;
        }

        bool isHorizontal = orientation == Orientation.Horizontal;

        for (int i = 0; i < children.Count; i++)
        {
            // Auto column/row for the child
            if (isHorizontal)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            }
            else
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            var child = children[i];
            if (isHorizontal)
            {
                Grid.SetColumn(child, i * 2);
            }
            else
            {
                Grid.SetRow(child, i * 2);
            }

            grid.Children.Add(child);

            // Star spacer between children (not after last)
            if (i < children.Count - 1)
            {
                if (isHorizontal)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
                }
                else
                {
                    grid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
                }
            }
        }

        // align maps to cross-axis alignment on each child
        ApplyCrossAxisAlignment(children, orientation, align);

        return grid;
    }

    /// <summary>
    /// Apply cross-axis alignment to each child control.
    /// For a horizontal row, cross-axis is vertical; for a vertical column, cross-axis is horizontal.
    /// </summary>
    private static void ApplyCrossAxisAlignment(List<Control> children, Orientation orientation, string? align)
    {
        if (align is null)
        {
            return;
        }

        foreach (var child in children)
        {
            if (orientation == Orientation.Horizontal)
            {
                child.VerticalAlignment = MapCrossAxisV(align);
            }
            else
            {
                child.HorizontalAlignment = MapCrossAxisH(align);
            }
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
