using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Xunit;

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

        Assert.IsType<Grid>(result.RootControl);
    }

    [AvaloniaFact]
    public void Grid_HasThreeColumnDefinitions_AutoStarAuto()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/2_row_layout.json");

        var grid = (Grid)result.RootControl;
        Assert.Equal(3, grid.ColumnDefinitions.Count);
        Assert.True(grid.ColumnDefinitions[0].Width.IsAuto);
        Assert.True(grid.ColumnDefinitions[1].Width.IsStar);
        Assert.True(grid.ColumnDefinitions[2].Width.IsAuto);
    }

    [AvaloniaFact]
    public void Grid_ContainsTwoTextBlocks_WithCorrectText()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/2_row_layout.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Equal(2, textBlocks.Count);
        Assert.Equal("Left Content", textBlocks[0].Text);
        Assert.Equal("Right Content", textBlocks[1].Text);
    }

    [AvaloniaFact]
    public void TextBlocks_HaveVerticalAlignmentCenter_CrossAxis()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/2_row_layout.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.All(textBlocks, tb => Assert.Equal(VerticalAlignment.Center, tb.VerticalAlignment));
    }

    [AvaloniaFact]
    public void TextBlocks_HaveCorrectVariantClasses()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/2_row_layout.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains("Body", textBlocks[0].Classes);
        Assert.Contains("Caption", textBlocks[1].Classes);
    }
}
