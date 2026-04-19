using Avalonia.Controls;
using Avalonia.Headless.XUnit;
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

        Assert.IsType<StackPanel>(result.RootControl);
    }

    [AvaloniaFact]
    public void Contains_TextField_ForInput()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/6_capitalized_text.json");

        TextBox? textBox = GalleryTestHelper.FindFirst<TextBox>(result.RootControl);
        Assert.NotNull(textBox);
    }

    [AvaloniaFact]
    public void Contains_CaptionLabel_CapitalizedOutput()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/6_capitalized_text.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(textBlocks, tb => tb.Text == "Capitalized output:" && tb.Classes.Contains("Caption"));
    }

    [AvaloniaFact]
    public void Contains_H2TextBlock()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/6_capitalized_text.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(textBlocks, tb => tb.Classes.Contains("Heading2"));
    }

    [AvaloniaFact]
    public void FunctionCallText_InitiallyEmpty_WhenNoInput()
    {
        // The h2 Text component has a function call value: { call: "capitalize", args: { value: path } }
        // With no input yet, capitalize("") returns "".
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/6_capitalized_text.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        TextBlock? h2 = textBlocks.FirstOrDefault(tb => tb.Classes.Contains("Heading2"));
        Assert.NotNull(h2);
        Assert.True(string.IsNullOrEmpty(h2.Text), $"Expected empty or null text but got \"{h2.Text}\"");
    }

    [AvaloniaFact]
    public void FunctionCallText_ShowsCapitalized_AfterTypingAndReRender()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/6_capitalized_text.json");

        TextBox? textBox = GalleryTestHelper.FindFirst<TextBox>(result.RootControl);
        Assert.NotNull(textBox);

        GalleryTestHelper.SetText(textBox, "hello");

        // Re-render to pick up updated data model through the capitalize function
        Control reRendered = result.ReRender();

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(reRendered);
        TextBlock? h2 = textBlocks.FirstOrDefault(tb => tb.Classes.Contains("Heading2"));
        Assert.NotNull(h2);
        Assert.Equal("Hello", h2.Text);
    }

    [AvaloniaFact]
    public void TextField_TwoWayBinding_StillWorks()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/6_capitalized_text.json");

        TextBox? textBox = GalleryTestHelper.FindFirst<TextBox>(result.RootControl);
        Assert.NotNull(textBox);

        GalleryTestHelper.SetText(textBox, "hello");

        Assert.Equal("hello", result.Surface.DataModel.Resolve(Core.Bindings.DynamicValue.FromPath("/inputValue")));
    }
}
