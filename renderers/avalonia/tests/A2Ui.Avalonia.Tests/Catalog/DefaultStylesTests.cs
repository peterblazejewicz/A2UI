using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class DefaultStylesTests
{
    [AvaloniaFact]
    public void TextCatalogEntry_Heading1_HasCorrectClass()
    {
        var entry = new TextCatalogEntry();
        var dm = new DataModel();
        var ctx = new MockRenderContext(dm);
        var component = new A2UiComponent
        {
            Id = "h1",
            Component = "Text",
            Text = DynamicValue.FromString("Title"),
            Variant = "h1",
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<TextBlock>();
        control.Classes.Should().Contain("Heading1");
    }

    [AvaloniaFact]
    public void TextCatalogEntry_Caption_HasCorrectClass()
    {
        var entry = new TextCatalogEntry();
        var dm = new DataModel();
        var ctx = new MockRenderContext(dm);
        var component = new A2UiComponent
        {
            Id = "cap",
            Component = "Text",
            Text = DynamicValue.FromString("Small"),
            Variant = "caption",
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<TextBlock>();
        control.Classes.Should().Contain("Caption");
    }

    [AvaloniaFact]
    public void TextCatalogEntry_DefaultVariant_HasBodyClass()
    {
        var entry = new TextCatalogEntry();
        var dm = new DataModel();
        var ctx = new MockRenderContext(dm);
        var component = new A2UiComponent
        {
            Id = "t1",
            Component = "Text",
            Text = DynamicValue.FromString("Body text"),
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<TextBlock>();
        control.Classes.Should().Contain("Body");
    }

    [AvaloniaFact]
    public void A2UiSurface_Constructor_LoadsDefaultStyles()
    {
        var surface = new Controls.A2UiSurface();

        surface.Styles.Should().NotBeEmpty("A2UiSurface should load A2UiDefaultStyles.axaml");
    }
}
