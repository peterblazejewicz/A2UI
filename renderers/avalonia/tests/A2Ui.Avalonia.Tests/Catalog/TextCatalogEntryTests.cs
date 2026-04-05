using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class TextCatalogEntryTests
{
    [AvaloniaFact]
    public void TextCatalogEntry_Create_WithLiteralText_SetsTextBlockText()
    {
        // Arrange
        var entry  = new TextCatalogEntry();
        var dm     = new DataModel();
        var ctx    = new MockRenderContext(dm);
        var component = new A2UiComponent
        {
            Id        = "t1",
            Component = "Text",
            Text      = DynamicValue.FromString("Hello World"),
        };

        // Act
        var control = entry.Create(component, dm, ctx);

        // Assert
        control.Should().BeOfType<TextBlock>();
        ((TextBlock)control).Text.Should().Be("Hello World");
    }

    [AvaloniaFact]
    public void TextCatalogEntry_Create_WithH1Variant_HasHeading1Class()
    {
        // Arrange
        var entry = new TextCatalogEntry();
        var dm    = new DataModel();
        var ctx   = new MockRenderContext(dm);
        var component = new A2UiComponent
        {
            Id        = "h1",
            Component = "Text",
            Text      = DynamicValue.FromString("Title"),
            Variant   = "h1",
        };

        // Act
        var control = entry.Create(component, dm, ctx);

        // Assert
        control.Classes.Should().Contain("Heading1");
    }

    [AvaloniaFact]
    public void TextCatalogEntry_Update_ChangesTextInPlace()
    {
        // Arrange
        var entry = new TextCatalogEntry();
        var dm    = new DataModel();
        var ctx   = new MockRenderContext(dm);
        var comp1 = new A2UiComponent { Id = "t1", Component = "Text",
                                         Text = DynamicValue.FromString("Before") };
        var comp2 = new A2UiComponent { Id = "t1", Component = "Text",
                                         Text = DynamicValue.FromString("After") };

        var control = entry.Create(comp1, dm, ctx);

        // Act
        bool updated = entry.Update(control, comp2, dm, ctx);

        // Assert
        updated.Should().BeTrue();
        ((TextBlock)control).Text.Should().Be("After");
    }
}