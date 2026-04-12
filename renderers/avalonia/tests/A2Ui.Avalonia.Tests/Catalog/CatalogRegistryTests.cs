using A2Ui.Avalonia.Catalog;
using FluentAssertions;
using Xunit;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class CatalogRegistryTests
{
    [Fact]
    public void CreateDefault_Registers18Types()
    {
        var registry = CatalogRegistry.CreateDefault();

        registry.RegisteredTypes.Should().HaveCount(18);
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

        registry.TryGetEntry(componentType, out var entry).Should().BeTrue();
        entry!.ComponentType.Should().Be(componentType);
    }

    [Fact]
    public void TryGetEntry_UnknownType_ReturnsFalse()
    {
        var registry = CatalogRegistry.CreateDefault();

        registry.TryGetEntry("NonExistent", out _).Should().BeFalse();
    }

    [Fact]
    public void Register_DuplicateType_Throws()
    {
        var registry = new CatalogRegistry();
        registry.Register(new TextCatalogEntry());

        var act = () => registry.Register(new TextCatalogEntry());

        act.Should().Throw<ArgumentException>();
    }
}
