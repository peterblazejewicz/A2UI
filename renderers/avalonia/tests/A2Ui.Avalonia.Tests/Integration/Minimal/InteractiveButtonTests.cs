using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

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

        result.RootControl.Should().BeOfType<StackPanel>();
        var panel = (StackPanel)result.RootControl;
        panel.Orientation.Should().Be(global::Avalonia.Layout.Orientation.Vertical);
    }

    [AvaloniaFact]
    public void Contains_TextBlock_ClickTheButtonBelow()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/3_interactive_button.json");

        List<TextBlock> textBlocks = GalleryTestHelper.FindAll<TextBlock>(result.RootControl);
        textBlocks.Should().Contain(tb => tb.Text == "Click the button below");
    }

    [AvaloniaFact]
    public void Contains_Button_WithAccentClass()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/3_interactive_button.json");

        Button? button = GalleryTestHelper.FindFirst<Button>(result.RootControl);
        button.Should().NotBeNull();
        button!.Classes.Should().Contain("accent");
    }

    [AvaloniaFact]
    public void Button_Content_IsTextBlock_ClickMe()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/3_interactive_button.json");

        Button? button = GalleryTestHelper.FindFirst<Button>(result.RootControl);
        button.Should().NotBeNull();
        button!.Content.Should().BeOfType<TextBlock>();
        var label = (TextBlock)button.Content!;
        label.Text.Should().Be("Click Me");
    }

    [AvaloniaFact]
    public void ClickButton_FiresButtonClickedAction()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/3_interactive_button.json");

        Button? button = GalleryTestHelper.FindFirst<Button>(result.RootControl);
        button.Should().NotBeNull();

        GalleryTestHelper.ClickButton(button!);

        result.ActionLog.Should().ContainSingle(a => a.EventName == "button_clicked");
    }

    [AvaloniaFact]
    public void ClickButton_PayloadIsNull_EmptyContextResolvesToNull()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/3_interactive_button.json");

        Button? button = GalleryTestHelper.FindFirst<Button>(result.RootControl);
        button.Should().NotBeNull();

        GalleryTestHelper.ClickButton(button!);

        UserActionEventArgs action = result.ActionLog.Single(a => a.EventName == "button_clicked");
        action.Payload.Should().BeNull();
    }
}
