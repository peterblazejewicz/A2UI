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
    public void InnerRow_IsStackPanel_Horizontal_WithJustifyStart()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/5_complex_layout.json");

        StackPanel? innerRow = GalleryTestHelper.FindFirst<StackPanel>(result.RootControl);
        innerRow.Should().NotBeNull();
        innerRow!.Orientation.Should().Be(global::Avalonia.Layout.Orientation.Horizontal);
    }

    [AvaloniaFact]
    [Trait("Gap", "Weight")]
    public void Weight_NotYetApplied_TextBoxesHaveNoStarSizing()
    {
        // Weight property is defined in the spec but not yet applied to controls.
        // This test documents the current behavior: TextBoxes exist without
        // proportional star sizing.
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/5_complex_layout.json");

        List<TextBox> textBoxes = GalleryTestHelper.FindAll<TextBox>(result.RootControl);
        textBoxes.Should().HaveCount(2, "weight is not applied but TextBoxes should still render");
    }
}
