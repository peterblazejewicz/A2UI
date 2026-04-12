using A2Ui.Core.Surfaces;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

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

        var tb = Assert.IsType<TextBlock>(result.RootControl);
        Assert.Equal("Hello, Minimal Catalog!", tb.Text);
    }

    [AvaloniaFact]
    public void Root_HasHeading1Class()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/1_simple_text.json");

        var tb = (TextBlock)result.RootControl;
        Assert.Contains("Heading1", tb.Classes);
    }

    [AvaloniaFact]
    public void DataModel_IsEmpty()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/1_simple_text.json");

        Assert.Equal("{}", result.Surface.DataModel.ToJson());
    }

    [AvaloniaFact]
    public void ActionLog_IsEmpty_NoActionsDispatched()
    {
        RenderResult result = GalleryTestHelper.ReplayExample("minimal/1_simple_text.json");

        Assert.Empty(result.ActionLog);
    }
}
