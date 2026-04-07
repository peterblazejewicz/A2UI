using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Integration.Minimal;

/// <summary>
/// Integration tests for minimal/2_row_layout.json.
/// Row with justify=spaceBetween, align=center, two Text children.
/// </summary>
public sealed class RowLayoutTests
{
    [AvaloniaFact]
    public void Root_IsGrid_ForSpaceBetweenJustify()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/2_row_layout.json");

        result.RootControl.Should().BeOfType<Grid>();
    }

    [AvaloniaFact]
    public void Grid_HasThreeColumnDefinitions_AutoStarAuto()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/2_row_layout.json");

        var grid = (Grid)result.RootControl;
        grid.ColumnDefinitions.Should().HaveCount(3);
        grid.ColumnDefinitions[0].Width.IsAuto.Should().BeTrue();
        grid.ColumnDefinitions[1].Width.IsStar.Should().BeTrue();
        grid.ColumnDefinitions[2].Width.IsAuto.Should().BeTrue();
    }

    [AvaloniaFact]
    public void Grid_ContainsTwoTextBlocks_WithCorrectText()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/2_row_layout.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        textBlocks.Should().HaveCount(2);
        textBlocks[0].Text.Should().Be("Left Content");
        textBlocks[1].Text.Should().Be("Right Content");
    }

    [AvaloniaFact]
    public void TextBlocks_HaveVerticalAlignmentCenter_CrossAxis()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/2_row_layout.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        textBlocks.Should().AllSatisfy(tb => tb.VerticalAlignment.Should().Be(VerticalAlignment.Center));
    }

    [AvaloniaFact]
    public void TextBlocks_HaveCorrectVariantClasses()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/2_row_layout.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        textBlocks[0].Classes.Should().Contain("Body");
        textBlocks[1].Classes.Should().Contain("Caption");
    }
}
