using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Integration.Minimal;

/// <summary>
/// Integration tests for minimal/1_simple_text.json.
/// One Text component: "Hello, Minimal Catalog!", variant h1, empty data model.
/// </summary>
public sealed class SimpleTextTests
{
    [AvaloniaFact]
    public void Root_IsTextBlock_WithCorrectText()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/1_simple_text.json");

        result.RootControl.Should().BeOfType<TextBlock>();
        var tb = (TextBlock)result.RootControl;
        tb.Text.Should().Be("Hello, Minimal Catalog!");
    }

    [AvaloniaFact]
    public void Root_HasHeading1Class()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/1_simple_text.json");

        var tb = (TextBlock)result.RootControl;
        tb.Classes.Should().Contain("Heading1");
    }

    [AvaloniaFact]
    public void DataModel_IsEmpty()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/1_simple_text.json");

        result.Surface.DataModel.ToJson().Should().Be("{}");
    }

    [AvaloniaFact]
    public void ActionLog_IsEmpty_NoActionsDispatched()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/1_simple_text.json");

        result.ActionLog.Should().BeEmpty();
    }
}
