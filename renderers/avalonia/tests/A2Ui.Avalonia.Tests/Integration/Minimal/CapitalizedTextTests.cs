using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using Xunit;

namespace A2Ui.Avalonia.Tests.Integration.Minimal;

/// <summary>
/// Integration tests for minimal/6_capitalized_text.json.
/// Column with TextField, caption "Capitalized output:", and Text with function call.
/// Function call resolution is not yet implemented (gap).
/// </summary>
public sealed class CapitalizedTextTests
{
    [AvaloniaFact]
    public void Root_IsStackPanel_ColumnWithJustifyStart()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/6_capitalized_text.json");

        result.RootControl.Should().BeOfType<StackPanel>();
    }

    [AvaloniaFact]
    public void Contains_TextField_ForInput()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/6_capitalized_text.json");

        TextBox? textBox = GalleryTestHelper.FindFirst<TextBox>(result.RootControl);
        textBox.Should().NotBeNull();
    }

    [AvaloniaFact]
    public void Contains_CaptionLabel_CapitalizedOutput()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/6_capitalized_text.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        textBlocks.Should().Contain(tb =>
            tb.Text == "Capitalized output:" && tb.Classes.Contains("Caption"));
    }

    [AvaloniaFact]
    public void Contains_H2TextBlock()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/6_capitalized_text.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        textBlocks.Should().Contain(tb => tb.Classes.Contains("Heading2"));
    }

    [AvaloniaFact]
    [Trait("Gap", "FunctionCall")]
    public void FunctionCallText_IsEmptyOrNull_NotYetResolved()
    {
        // The h2 Text component has a function call value: { call: "capitalize", args: ... }
        // Function call resolution is not yet implemented, so the text should be empty.
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/6_capitalized_text.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        TextBlock? h2 = textBlocks.FirstOrDefault(tb => tb.Classes.Contains("Heading2"));
        h2.Should().NotBeNull();
        h2!.Text.Should().BeOneOf("", null, string.Empty);
    }

    [AvaloniaFact]
    public void TextField_TwoWayBinding_StillWorks()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/6_capitalized_text.json");

        TextBox? textBox = GalleryTestHelper.FindFirst<TextBox>(result.RootControl);
        textBox.Should().NotBeNull();

        GalleryTestHelper.SetText(textBox!, "hello");

        result.Surface.DataModel.Resolve(
            new A2Ui.Core.Messages.DynamicValue { Path = "/inputValue" }).Should().Be("hello");
    }
}
