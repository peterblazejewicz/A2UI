using A2Ui.Avalonia.Catalog.Entries;
using A2Ui.Core.Bindings;
using A2Ui.Core.Components;
using A2Ui.Core.Surfaces;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class TextCatalogEntryTests
{
    [AvaloniaFact]
    public void TextCatalogEntry_Create_WithLiteralText_SetsTextBlockText()
    {
        // Arrange
        var entry = new TextCatalogEntry();
        var dm = new DataModel();
        var ctx = new MockRenderContext(dm);
        var component = new TextComponent { Id = "t1", Text = DynamicValue.FromString("Hello World") };

        // Act
        var control = entry.Create(component, dm, ctx);

        // Assert
        Assert.IsType<TextBlock>(control);
        Assert.Equal("Hello World", ((TextBlock)control).Text);
    }

    [AvaloniaFact]
    public void TextCatalogEntry_Create_WithH1Variant_HasHeading1Class()
    {
        // Arrange
        var entry = new TextCatalogEntry();
        var dm = new DataModel();
        var ctx = new MockRenderContext(dm);
        var component = new TextComponent
        {
            Id = "h1",
            Text = DynamicValue.FromString("Title"),
            Variant = "h1",
        };

        // Act
        var control = entry.Create(component, dm, ctx);

        // Assert
        Assert.Contains("Heading1", control.Classes);
    }

    [AvaloniaFact]
    public void TextCatalogEntry_Update_ChangesTextInPlace()
    {
        // Arrange
        var entry = new TextCatalogEntry();
        var dm = new DataModel();
        var ctx = new MockRenderContext(dm);
        var comp1 = new TextComponent { Id = "t1", Text = DynamicValue.FromString("Before") };
        var comp2 = new TextComponent { Id = "t1", Text = DynamicValue.FromString("After") };

        var control = entry.Create(comp1, dm, ctx);

        // Act
        bool updated = entry.Update(control, comp2, dm, ctx);

        // Assert
        Assert.True(updated);
        Assert.Equal("After", ((TextBlock)control).Text);
    }
}
