using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using FluentAssertions;
using Microsoft.Extensions.Logging;

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

        var component = new A2UiComponent
        {
            Id = "row1",
            Component = "Row",
            Children = ChildList.FromIds(["c1", "c2"]),
        };

        var control = entry.Create(component, dm, ctx);

        // Default layout is StackPanel for non-spaceBetween
        control.Should().BeAssignableTo<StackPanel>();
        var panel = (StackPanel)control;
        panel.Orientation.Should().Be(Orientation.Horizontal);
        panel.Children.Should().HaveCount(2);
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

        var component = new A2UiComponent
        {
            Id = "row1",
            Component = "Row",
            Children = ChildList.FromIds(["c1", "c2"]),
            Justify = "spaceBetween",
            Align = "center",
        };

        var control = entry.Create(component, dm, ctx);

        // spaceBetween uses a Grid with star spacers
        control.Should().BeOfType<Grid>();
        var grid = (Grid)control;
        grid.Children.Should().HaveCount(2);
        // 2 children → 2 auto columns + 1 star spacer = 3 column definitions
        grid.ColumnDefinitions.Should().HaveCount(3);
        grid.ColumnDefinitions[1].Width.IsStar.Should().BeTrue();
    }

    [AvaloniaFact]
    public void RowCatalogEntry_Create_Center_SetsHorizontalAlignmentCenter()
    {
        var entry = new RowCatalogEntry();
        var dm = new DataModel();
        var ctx = new ChildReturningRenderContext(dm, new TextBlock { Text = "Child" });

        var component = new A2UiComponent
        {
            Id = "row1",
            Component = "Row",
            Children = ChildList.FromIds(["c1"]),
            Justify = "center",
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeAssignableTo<StackPanel>();
        var panel = (StackPanel)control;
        panel.HorizontalAlignment.Should().Be(HorizontalAlignment.Center);
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

        var component = new A2UiComponent
        {
            Id = "col1",
            Component = "Column",
            Children = ChildList.FromIds(["c1", "c2"]),
            Justify = "start",
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeAssignableTo<StackPanel>();
        var panel = (StackPanel)control;
        panel.Orientation.Should().Be(Orientation.Vertical);
        panel.Children.Should().HaveCount(2);
        panel.VerticalAlignment.Should().Be(VerticalAlignment.Top);
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

        var component = new A2UiComponent
        {
            Id = "col1",
            Component = "Column",
            Children = ChildList.FromIds(["c1", "c2"]),
            Justify = "spaceBetween",
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<Grid>();
        var grid = (Grid)control;
        grid.Children.Should().HaveCount(2);
        // Vertical spaceBetween → row definitions
        grid.RowDefinitions.Should().HaveCount(3);
        grid.RowDefinitions[1].Height.IsStar.Should().BeTrue();
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

        var component = new A2UiComponent
        {
            Id = "row1",
            Component = "Row",
            Children = ChildList.FromIds(["c1", "c2"]),
            Align = "center",
        };

        entry.Create(component, dm, ctx);

        // Cross-axis for horizontal row is vertical
        child1.VerticalAlignment.Should().Be(VerticalAlignment.Center);
        child2.VerticalAlignment.Should().Be(VerticalAlignment.Center);
    }

    [AvaloniaFact]
    public void ColumnCatalogEntry_Create_AlignCenter_SetsChildHorizontalAlignmentCenter()
    {
        var child1 = new TextBlock { Text = "A" };
        var child2 = new TextBlock { Text = "B" };
        var entry = new ColumnCatalogEntry();
        var dm = new DataModel();
        var ctx = new ChildReturningRenderContext(dm, child1, child2);

        var component = new A2UiComponent
        {
            Id = "col1",
            Component = "Column",
            Children = ChildList.FromIds(["c1", "c2"]),
            Align = "center",
        };

        entry.Create(component, dm, ctx);

        // Cross-axis for vertical column is horizontal
        child1.HorizontalAlignment.Should().Be(HorizontalAlignment.Center);
        child2.HorizontalAlignment.Should().Be(HorizontalAlignment.Center);
    }

    [AvaloniaFact]
    public void RowCatalogEntry_Create_SpaceBetween_ZeroChildren_ReturnsEmptyGrid()
    {
        var entry = new RowCatalogEntry();
        var dm = new DataModel();
        var ctx = new ChildReturningRenderContext(dm); // no children

        var component = new A2UiComponent
        {
            Id = "row2",
            Component = "Row",
            Children = ChildList.FromIds([]),
            Justify = "spaceBetween",
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<Grid>();
        var grid = (Grid)control;
        grid.Children.Should().BeEmpty();
        grid.ColumnDefinitions.Should().BeEmpty();
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

        var component = new A2UiComponent
        {
            Id = "row1",
            Component = "Row",
            Children = ChildList.FromIds(["c1", "c2"]),
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<Grid>("equal-weight children should produce a star-sized Grid");
        var grid = (Grid)control;
        grid.ColumnDefinitions.Should().HaveCount(2);
        grid.ColumnDefinitions[0].Width.IsStar.Should().BeTrue();
        grid.ColumnDefinitions[0].Width.Value.Should().Be(1);
        grid.ColumnDefinitions[1].Width.IsStar.Should().BeTrue();
        grid.ColumnDefinitions[1].Width.Value.Should().Be(1);
        grid.Children.Should().HaveCount(2);
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

        var component = new A2UiComponent
        {
            Id = "row1",
            Component = "Row",
            Children = ChildList.FromIds(["c1", "c2"]),
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<Grid>();
        var grid = (Grid)control;
        grid.ColumnDefinitions.Should().HaveCount(2);
        grid.ColumnDefinitions[0].Width.Value.Should().Be(2, "c1 has weight 2");
        grid.ColumnDefinitions[1].Width.Value.Should().Be(1, "c2 has weight 1");
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

        var component = new A2UiComponent
        {
            Id = "row1",
            Component = "Row",
            Children = ChildList.FromIds(["c1", "c2"]),
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<Grid>();
        var grid = (Grid)control;
        grid.ColumnDefinitions.Should().HaveCount(2);
        grid.ColumnDefinitions[0].Width.IsAuto.Should().BeTrue("weight 0 gets Auto sizing");
        grid.ColumnDefinitions[1].Width.IsStar.Should().BeTrue("weight 1 gets Star sizing");
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

        var component = new A2UiComponent
        {
            Id = "col1",
            Component = "Column",
            Children = ChildList.FromIds(["r1", "r2"]),
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<Grid>("weight children in Column should produce a star-sized Grid");
        var grid = (Grid)control;
        grid.RowDefinitions.Should().HaveCount(2);
        grid.RowDefinitions[0].Height.IsStar.Should().BeTrue();
        grid.RowDefinitions[0].Height.Value.Should().Be(1);
        grid.RowDefinitions[1].Height.IsStar.Should().BeTrue();
        grid.RowDefinitions[1].Height.Value.Should().Be(3);
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

        var component = new A2UiComponent
        {
            Id = "row1",
            Component = "Row",
            Children = ChildList.FromIds(["c1", "c2"]),
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeAssignableTo<StackPanel>("no weights means StackPanel is used");
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
}
