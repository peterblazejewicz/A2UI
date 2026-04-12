using A2Ui.Avalonia.Catalog;
using Xunit;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class CatalogRegistryTests
{
    [Fact]
    public void CreateDefault_Registers18Types()
    {
        var registry = CatalogRegistry.CreateDefault();

        Assert.Equal(18, registry.RegisteredTypes.Count);
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("Image")]
    [InlineData("Icon")]
    [InlineData("Video")]
    [InlineData("AudioPlayer")]
    [InlineData("Divider")]
    [InlineData("Row")]
    [InlineData("Column")]
    [InlineData("List")]
    [InlineData("Card")]
    [InlineData("Tabs")]
    [InlineData("Modal")]
    [InlineData("Button")]
    [InlineData("TextField")]
    [InlineData("CheckBox")]
    [InlineData("ChoicePicker")]
    [InlineData("DateTimeInput")]
    [InlineData("Slider")]
    public void CreateDefault_ContainsSpecType(string componentType)
    {
        var registry = CatalogRegistry.CreateDefault();

        Assert.True(registry.TryGetEntry(componentType, out var entry));
        Assert.Equal(componentType, entry!.ComponentType);
    }

    [Fact]
    public void TryGetEntry_UnknownType_ReturnsFalse()
    {
        var registry = CatalogRegistry.CreateDefault();

        Assert.False(registry.TryGetEntry("NonExistent", out _));
    }

    [Fact]
    public void Register_DuplicateType_Throws()
    {
        var registry = new CatalogRegistry();
        registry.Register(new TextCatalogEntry());

        Assert.Throws<ArgumentException>(() => registry.Register(new TextCatalogEntry()));
    }
}
