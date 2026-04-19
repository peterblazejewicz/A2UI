using A2Ui.Avalonia.Catalog;
using A2Ui.Avalonia.Catalog.Entries;
using A2Ui.Core;
using A2Ui.Core.Bindings;
using A2Ui.Core.Children;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;
using A2Ui.Core.Surfaces;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Microsoft.Extensions.Logging;
using Xunit;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class LayoutCatalogEntryTests
{
    // ── Row ───────────────────────────────────────────────────

    [AvaloniaFact]
    public void RowCatalogEntry_Create_RendersChildrenHorizontally()
    {
        var entry = new RowCatalogEntry();
        var dm = new DataModel();
        var ctx = new ChildReturningRenderContext(
            dm,
            new TextBlock { Text = "Left" },
            new TextBlock { Text = "Right" }
        );

        var component = new RowComponent { Id = "row1", Children = ChildList.FromIds(["c1", "c2"]) };

        var control = entry.Create(component, dm, ctx);

        // Default layout is StackPanel for non-spaceBetween
        Assert.IsAssignableFrom<StackPanel>(control);
        var panel = (StackPanel)control;
        Assert.Equal(Orientation.Horizontal, panel.Orientation);
        Assert.Equal(2, panel.Children.Count);
    }

    [AvaloniaFact]
    public void RowCatalogEntry_Create_SpaceBetween_UsesGrid()
    {
        var entry = new RowCatalogEntry();
        var dm = new DataModel();
        var ctx = new ChildReturningRenderContext(
            dm,
            new TextBlock { Text = "Left" },
            new TextBlock { Text = "Right" }
        );

        var component = new RowComponent
        {
            Id = "row1",
            Children = ChildList.FromIds(["c1", "c2"]),
            Justify = "spaceBetween",
            Align = "center",
        };

        var control = entry.Create(component, dm, ctx);

        // spaceBetween uses a Grid with star spacers
        Assert.IsType<Grid>(control);
        var grid = (Grid)control;
        Assert.Equal(2, grid.Children.Count);
        // 2 children -> 2 auto columns + 1 star spacer = 3 column definitions
        Assert.Equal(3, grid.ColumnDefinitions.Count);
        Assert.True(grid.ColumnDefinitions[1].Width.IsStar);
    }

    [AvaloniaFact]
    public void RowCatalogEntry_Create_Center_SetsHorizontalAlignmentCenter()
    {
        var entry = new RowCatalogEntry();
        var dm = new DataModel();
        var ctx = new ChildReturningRenderContext(dm, new TextBlock { Text = "Child" });

        var component = new RowComponent
        {
            Id = "row1",
            Children = ChildList.FromIds(["c1"]),
            Justify = "center",
        };

        var control = entry.Create(component, dm, ctx);

        Assert.IsAssignableFrom<StackPanel>(control);
        var panel = (StackPanel)control;
        Assert.Equal(HorizontalAlignment.Center, panel.HorizontalAlignment);
    }

    // ── Column ────────────────────────────────────────────────

    [AvaloniaFact]
    public void ColumnCatalogEntry_Create_RendersChildrenVertically()
    {
        var entry = new ColumnCatalogEntry();
        var dm = new DataModel();
        var ctx = new ChildReturningRenderContext(
            dm,
            new TextBlock { Text = "Top" },
            new TextBlock { Text = "Bottom" }
        );

        var component = new ColumnComponent
        {
            Id = "col1",
            Children = ChildList.FromIds(["c1", "c2"]),
            Justify = "start",
        };

        var control = entry.Create(component, dm, ctx);

        Assert.IsAssignableFrom<StackPanel>(control);
        var panel = (StackPanel)control;
        Assert.Equal(Orientation.Vertical, panel.Orientation);
        Assert.Equal(2, panel.Children.Count);
        Assert.Equal(VerticalAlignment.Top, panel.VerticalAlignment);
    }

    [AvaloniaFact]
    public void ColumnCatalogEntry_Create_SpaceBetween_UsesGrid()
    {
        var entry = new ColumnCatalogEntry();
        var dm = new DataModel();
        var ctx = new ChildReturningRenderContext(
            dm,
            new TextBlock { Text = "Top" },
            new TextBlock { Text = "Bottom" }
        );

        var component = new ColumnComponent
        {
            Id = "col1",
            Children = ChildList.FromIds(["c1", "c2"]),
            Justify = "spaceBetween",
        };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<Grid>(control);
        var grid = (Grid)control;
        Assert.Equal(2, grid.Children.Count);
        // Vertical spaceBetween -> row definitions
        Assert.Equal(3, grid.RowDefinitions.Count);
        Assert.True(grid.RowDefinitions[1].Height.IsStar);
    }

    // ── Cross-axis alignment ──────────────────────────────────

    [AvaloniaFact]
    public void RowCatalogEntry_Create_AlignCenter_SetsChildVerticalAlignmentCenter()
    {
        var child1 = new TextBlock { Text = "A" };
        var child2 = new TextBlock { Text = "B" };
        var entry = new RowCatalogEntry();
        var dm = new DataModel();
        var ctx = new ChildReturningRenderContext(dm, child1, child2);

        var component = new RowComponent
        {
            Id = "row1",
            Children = ChildList.FromIds(["c1", "c2"]),
            Align = "center",
        };

        entry.Create(component, dm, ctx);

        // Cross-axis for horizontal row is vertical
        Assert.Equal(VerticalAlignment.Center, child1.VerticalAlignment);
        Assert.Equal(VerticalAlignment.Center, child2.VerticalAlignment);
    }

    [AvaloniaFact]
    public void ColumnCatalogEntry_Create_AlignCenter_SetsChildHorizontalAlignmentCenter()
    {
        var child1 = new TextBlock { Text = "A" };
        var child2 = new TextBlock { Text = "B" };
        var entry = new ColumnCatalogEntry();
        var dm = new DataModel();
        var ctx = new ChildReturningRenderContext(dm, child1, child2);

        var component = new ColumnComponent
        {
            Id = "col1",
            Children = ChildList.FromIds(["c1", "c2"]),
            Align = "center",
        };

        entry.Create(component, dm, ctx);

        // Cross-axis for vertical column is horizontal
        Assert.Equal(HorizontalAlignment.Center, child1.HorizontalAlignment);
        Assert.Equal(HorizontalAlignment.Center, child2.HorizontalAlignment);
    }

    [AvaloniaFact]
    public void RowCatalogEntry_Create_SpaceBetween_ZeroChildren_ReturnsEmptyGrid()
    {
        var entry = new RowCatalogEntry();
        var dm = new DataModel();
        var ctx = new ChildReturningRenderContext(dm); // no children

        var component = new RowComponent
        {
            Id = "row2",
            Children = ChildList.FromIds([]),
            Justify = "spaceBetween",
        };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<Grid>(control);
        var grid = (Grid)control;
        Assert.Empty(grid.Children);
        Assert.Empty(grid.ColumnDefinitions);
    }

    // ── Weight (star-sized proportional layout) ───────────────

    [AvaloniaFact]
    public void RowCatalogEntry_Create_EqualWeights_UsesStarGrid()
    {
        var entry = new RowCatalogEntry();
        var dm = new DataModel();
        var ctx = new WeightedChildRenderContext(
            dm,
            new Dictionary<string, double> { ["c1"] = 1, ["c2"] = 1 },
            new TextBlock { Text = "Left" },
            new TextBlock { Text = "Right" }
        );

        var component = new RowComponent { Id = "row1", Children = ChildList.FromIds(["c1", "c2"]) };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<Grid>(control);
        var grid = (Grid)control;
        Assert.Equal(2, grid.ColumnDefinitions.Count);
        Assert.True(grid.ColumnDefinitions[0].Width.IsStar);
        Assert.Equal(1, grid.ColumnDefinitions[0].Width.Value);
        Assert.True(grid.ColumnDefinitions[1].Width.IsStar);
        Assert.Equal(1, grid.ColumnDefinitions[1].Width.Value);
        Assert.Equal(2, grid.Children.Count);
    }

    [AvaloniaFact]
    public void RowCatalogEntry_Create_UnequalWeights_UsesProportionalStarColumns()
    {
        var entry = new RowCatalogEntry();
        var dm = new DataModel();
        var ctx = new WeightedChildRenderContext(
            dm,
            new Dictionary<string, double> { ["c1"] = 2, ["c2"] = 1 },
            new TextBlock { Text = "Wide" },
            new TextBlock { Text = "Narrow" }
        );

        var component = new RowComponent { Id = "row1", Children = ChildList.FromIds(["c1", "c2"]) };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<Grid>(control);
        var grid = (Grid)control;
        Assert.Equal(2, grid.ColumnDefinitions.Count);
        Assert.Equal(2, grid.ColumnDefinitions[0].Width.Value);
        Assert.Equal(1, grid.ColumnDefinitions[1].Width.Value);
    }

    [AvaloniaFact]
    public void RowCatalogEntry_Create_MixedWeights_ZeroWeightGetsAuto()
    {
        var entry = new RowCatalogEntry();
        var dm = new DataModel();
        var ctx = new WeightedChildRenderContext(
            dm,
            new Dictionary<string, double> { ["c1"] = 0, ["c2"] = 1 },
            new TextBlock { Text = "Auto" },
            new TextBlock { Text = "Star" }
        );

        var component = new RowComponent { Id = "row1", Children = ChildList.FromIds(["c1", "c2"]) };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<Grid>(control);
        var grid = (Grid)control;
        Assert.Equal(2, grid.ColumnDefinitions.Count);
        Assert.True(grid.ColumnDefinitions[0].Width.IsAuto);
        Assert.True(grid.ColumnDefinitions[1].Width.IsStar);
    }

    [AvaloniaFact]
    public void ColumnCatalogEntry_Create_EqualWeights_UsesStarGridRows()
    {
        var entry = new ColumnCatalogEntry();
        var dm = new DataModel();
        var ctx = new WeightedChildRenderContext(
            dm,
            new Dictionary<string, double> { ["r1"] = 1, ["r2"] = 3 },
            new TextBlock { Text = "Top" },
            new TextBlock { Text = "Bottom" }
        );

        var component = new ColumnComponent { Id = "col1", Children = ChildList.FromIds(["r1", "r2"]) };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<Grid>(control);
        var grid = (Grid)control;
        Assert.Equal(2, grid.RowDefinitions.Count);
        Assert.True(grid.RowDefinitions[0].Height.IsStar);
        Assert.Equal(1, grid.RowDefinitions[0].Height.Value);
        Assert.True(grid.RowDefinitions[1].Height.IsStar);
        Assert.Equal(3, grid.RowDefinitions[1].Height.Value);
    }

    [AvaloniaFact]
    public void RowCatalogEntry_Create_NoWeights_FallsBackToStackPanel()
    {
        // When all weights are null/0, normal StackPanel layout is used
        var entry = new RowCatalogEntry();
        var dm = new DataModel();
        var ctx = new ChildReturningRenderContext(
            dm,
            new TextBlock { Text = "Left" },
            new TextBlock { Text = "Right" }
        );

        var component = new RowComponent { Id = "row1", Children = ChildList.FromIds(["c1", "c2"]) };

        var control = entry.Create(component, dm, ctx);

        Assert.IsAssignableFrom<StackPanel>(control);
    }
}

/// <summary>
/// A render context that returns pre-built child controls in order,
/// allowing layout entry tests without a full Surface/Renderer pipeline.
/// </summary>
internal sealed class ChildReturningRenderContext : IRenderContext
{
    private readonly DataModel _dm;
    private readonly Queue<Control> _children;

    public ChildReturningRenderContext(DataModel dm, params Control[] children)
    {
        this._dm = dm;
        this._children = new Queue<Control>(children);
    }

    public Control? RenderChild(string? childId) => this._children.Count > 0 ? this._children.Dequeue() : null;

    public IEnumerable<Control> RenderChildren(string parentId)
    {
        while (this._children.Count > 0)
        {
            yield return this._children.Dequeue();
        }
    }

    public void FireUserAction(string eventName, object? payload = null, string? componentId = null) { }

    public string? Resolve(DynamicValue? value) => this._dm.Resolve(value);

    public void UpdateDataModel(string path, string? value) { }

    public double? GetComponentWeight(string componentId) => null;

    public ILogger? Logger => null;

    public CancellationToken SurfaceCancellation => CancellationToken.None;
}

/// <summary>
/// A render context with configurable per-ID weight values for testing weighted layouts.
/// </summary>
internal sealed class WeightedChildRenderContext : IRenderContext
{
    private readonly DataModel _dm;
    private readonly Queue<Control> _children;
    private readonly Dictionary<string, double> _weights;

    public WeightedChildRenderContext(DataModel dm, Dictionary<string, double> weights, params Control[] children)
    {
        this._dm = dm;
        this._weights = weights;
        this._children = new Queue<Control>(children);
    }

    public Control? RenderChild(string? childId) => this._children.Count > 0 ? this._children.Dequeue() : null;

    public IEnumerable<Control> RenderChildren(string parentId)
    {
        while (this._children.Count > 0)
        {
            yield return this._children.Dequeue();
        }
    }

    public void FireUserAction(string eventName, object? payload = null, string? componentId = null) { }

    public string? Resolve(DynamicValue? value) => this._dm.Resolve(value);

    public void UpdateDataModel(string path, string? value) { }

    public double? GetComponentWeight(string componentId) =>
        this._weights.TryGetValue(componentId, out var w) ? w : null;

    public ILogger? Logger => null;

    public CancellationToken SurfaceCancellation => CancellationToken.None;
}
