using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class LayoutCatalogEntryTests
{
    // ── Row ───────────────────────────────────────────────────

    [AvaloniaFact]
    public void RowCatalogEntry_Create_RendersChildrenHorizontally()
    {
        var entry = new RowCatalogEntry();
        var dm = new DataModel();
        var ctx = new ChildReturningRenderContext(dm,
            new TextBlock { Text = "Left" },
            new TextBlock { Text = "Right" });

        var component = new A2UiComponent
        {
            Id = "row1", Component = "Row",
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
        var ctx = new ChildReturningRenderContext(dm,
            new TextBlock { Text = "Left" },
            new TextBlock { Text = "Right" });

        var component = new A2UiComponent
        {
            Id = "row1", Component = "Row",
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
            Id = "row1", Component = "Row",
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
        var ctx = new ChildReturningRenderContext(dm,
            new TextBlock { Text = "Top" },
            new TextBlock { Text = "Bottom" });

        var component = new A2UiComponent
        {
            Id = "col1", Component = "Column",
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
        var ctx = new ChildReturningRenderContext(dm,
            new TextBlock { Text = "Top" },
            new TextBlock { Text = "Bottom" });

        var component = new A2UiComponent
        {
            Id = "col1", Component = "Column",
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
            Id = "row1", Component = "Row",
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
            Id = "col1", Component = "Column",
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
            Id = "row2", Component = "Row",
            Children = ChildList.FromIds([]),
            Justify = "spaceBetween",
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<Grid>();
        var grid = (Grid)control;
        grid.Children.Should().BeEmpty();
        grid.ColumnDefinitions.Should().BeEmpty();
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
        _dm = dm;
        _children = new Queue<Control>(children);
    }

    public Control? RenderChild(string? childId) =>
        _children.Count > 0 ? _children.Dequeue() : null;

    public IEnumerable<Control> RenderChildren(string parentId)
    {
        while (_children.Count > 0)
            yield return _children.Dequeue();
    }

    public void FireUserAction(string eventName, object? payload = null, string? componentId = null) { }
    public string? Resolve(DynamicValue? value) => _dm.Resolve(value);
    public void UpdateDataModel(string path, string? value) { }
}
