using A2Ui.Avalonia.Catalog;
using A2Ui.Avalonia.Catalog.Entries;
using A2Ui.Core;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;
using A2Ui.Core.Surfaces;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class DefaultStylesTests
{
    [AvaloniaFact]
    public void TextCatalogEntry_Heading1_HasCorrectClass()
    {
        var entry = new TextCatalogEntry();
        var dm = new DataModel();
        var ctx = new MockRenderContext(dm);
        var component = new TextComponent
        {
            Id = "h1",
            Text = DynamicValue.FromString("Title"),
            Variant = "h1",
        };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<TextBlock>(control);
        Assert.Contains("Heading1", control.Classes);
    }

    [AvaloniaFact]
    public void TextCatalogEntry_Caption_HasCorrectClass()
    {
        var entry = new TextCatalogEntry();
        var dm = new DataModel();
        var ctx = new MockRenderContext(dm);
        var component = new TextComponent
        {
            Id = "cap",
            Text = DynamicValue.FromString("Small"),
            Variant = "caption",
        };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<TextBlock>(control);
        Assert.Contains("Caption", control.Classes);
    }

    [AvaloniaFact]
    public void TextCatalogEntry_DefaultVariant_HasBodyClass()
    {
        var entry = new TextCatalogEntry();
        var dm = new DataModel();
        var ctx = new MockRenderContext(dm);
        var component = new TextComponent { Id = "t1", Text = DynamicValue.FromString("Body text") };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<TextBlock>(control);
        Assert.Contains("Body", control.Classes);
    }

    [AvaloniaFact]
    public void A2UiSurface_Constructor_LoadsDefaultStyles()
    {
        var surface = new Controls.A2UiSurface();

        Assert.NotEmpty(surface.Styles);
    }
}
