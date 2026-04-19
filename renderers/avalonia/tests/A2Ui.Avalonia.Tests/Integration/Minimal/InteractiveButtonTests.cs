using A2Ui.Core.Actions;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace A2Ui.Avalonia.Tests.Integration.Minimal;

/// <summary>
/// Integration tests for minimal/3_interactive_button.json.
/// Column (center/center) with Text + Button (primary, action button_clicked).
/// </summary>
public sealed class InteractiveButtonTests
{
    [AvaloniaFact]
    public void Root_IsStackPanel_Vertical()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/3_interactive_button.json");

        var panel = Assert.IsType<StackPanel>(result.RootControl);
        Assert.Equal(global::Avalonia.Layout.Orientation.Vertical, panel.Orientation);
    }

    [AvaloniaFact]
    public void Contains_TextBlock_ClickTheButtonBelow()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/3_interactive_button.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        Assert.Contains(textBlocks, tb => tb.Text == "Click the button below");
    }

    [AvaloniaFact]
    public void Contains_Button_WithAccentClass()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/3_interactive_button.json");

        Button? button = GalleryTestHelper.FindFirst<Button>(result.RootControl);
        Assert.NotNull(button);
        Assert.Contains("accent", button.Classes);
    }

    [AvaloniaFact]
    public void Button_Content_IsTextBlock_ClickMe()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/3_interactive_button.json");

        Button? button = GalleryTestHelper.FindFirst<Button>(result.RootControl);
        Assert.NotNull(button);
        var label = Assert.IsType<TextBlock>(button.Content);
        Assert.Equal("Click Me", label.Text);
    }

    [AvaloniaFact]
    public void ClickButton_FiresButtonClickedAction()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/3_interactive_button.json");

        Button? button = GalleryTestHelper.FindFirst<Button>(result.RootControl);
        Assert.NotNull(button);

        GalleryTestHelper.ClickButton(button);

        Assert.Single(result.ActionLog, a => a.EventName == "button_clicked");
    }

    [AvaloniaFact]
    public void ClickButton_PayloadIsNull_EmptyContextResolvesToNull()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/3_interactive_button.json");

        Button? button = GalleryTestHelper.FindFirst<Button>(result.RootControl);
        Assert.NotNull(button);

        GalleryTestHelper.ClickButton(button);

        UserActionEventArgs action = result.ActionLog.Single(a => a.EventName == "button_clicked");
        Assert.Null(action.Payload);
    }
}
