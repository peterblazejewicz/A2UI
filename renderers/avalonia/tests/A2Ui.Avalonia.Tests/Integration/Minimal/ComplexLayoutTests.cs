using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using Xunit;

namespace A2Ui.Avalonia.Tests.Integration.Minimal;

/// <summary>
/// Integration tests for minimal/5_complex_layout.json.
/// Column (spaceBetween/stretch) with h1, Row (start/start) with 2 TextFields, caption.
/// </summary>
public sealed class ComplexLayoutTests
{
    [AvaloniaFact]
    public void Root_IsGrid_ColumnWithSpaceBetween()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/5_complex_layout.json");

        result.RootControl.Should().BeOfType<Grid>();
    }

    [AvaloniaFact]
    public void Contains_HeaderTextBlock_H1()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/5_complex_layout.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        textBlocks.Should().Contain(tb => tb.Text == "User Profile Form" && tb.Classes.Contains("Heading1"));
    }

    [AvaloniaFact]
    public void Contains_TwoTextBoxes_WithCorrectWatermarks()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/5_complex_layout.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        textBoxes.Should().HaveCount(2);
        textBoxes[0].Watermark.Should().Be("First Name");
        textBoxes[1].Watermark.Should().Be("Last Name");
    }

    [AvaloniaFact]
    public void Contains_FooterTextBlock_Caption()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/5_complex_layout.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        textBlocks.Should().Contain(tb =>
            tb.Text == "Please fill out all fields." && tb.Classes.Contains("Caption"));
    }

    [AvaloniaFact]
    public void InnerRow_IsGrid_BecauseChildrenHaveWeights()
    {
        // The form_row Row has first_name/last_name with weight:1 — it should
        // render as a star-sized Grid, not a StackPanel.
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/5_complex_layout.json");

        List<Grid> grids = GalleryTestHelper.FindAll<Grid>(result.RootControl);
        Grid? innerRow = grids.FirstOrDefault(
            g => g.ColumnDefinitions.Count == 2);

        innerRow.Should().NotBeNull("form_row with weight:1 children must produce a Grid");
        innerRow!.ColumnDefinitions.All(cd => cd.Width.IsStar).Should().BeTrue();
    }

    [AvaloniaFact]
    public void InnerRow_UsesWeightedGrid_BothTextFieldsGetStarSizing()
    {
        // first_name and last_name both have weight:1 → inner Row should be a
        // proportional star-sized Grid, not a StackPanel.
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/5_complex_layout.json");

        // Find the Grid that has exactly 2 star-sized column definitions (the weighted row)
        List<Grid> allGrids = GalleryTestHelper.FindAll<Grid>(result.RootControl);
        Grid? weightedGrid = allGrids.FirstOrDefault(
            g => g.ColumnDefinitions.Count == 2 && g.ColumnDefinitions.All(cd => cd.Width.IsStar));

        weightedGrid.Should().NotBeNull("Row with weight:1 children should use a star-sized Grid");
        weightedGrid!.ColumnDefinitions.Should().HaveCount(2);
        weightedGrid.ColumnDefinitions[0].Width.Value.Should().Be(1, "first_name has weight 1");
        weightedGrid.ColumnDefinitions[1].Width.Value.Should().Be(1, "last_name has weight 1");
        weightedGrid.Children.Should().HaveCount(2);
    }
}
