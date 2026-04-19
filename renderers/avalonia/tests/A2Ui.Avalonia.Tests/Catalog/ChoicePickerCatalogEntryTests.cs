using A2Ui.Avalonia.Catalog;
using A2Ui.Avalonia.Catalog.Entries;
using A2Ui.Core;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;
using A2Ui.Core.Surfaces;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Xunit;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class ChoicePickerCatalogEntryTests
{
    private static (DataModel dm, MockRenderContext ctx) Setup()
    {
        var dm = new DataModel();
        return (dm, new MockRenderContext(dm));
    }

    [AvaloniaFact]
    public void Create_MutuallyExclusive_CreatesComboBox()
    {
        var (dm, ctx) = Setup();
        var entry = new ChoicePickerCatalogEntry();
        var component = new ChoicePickerComponent
        {
            Id = "cp1",
            Options =
            [
                new ChoiceOption { Label = DynamicValue.FromString("Red"), Value = "red" },
                new ChoiceOption { Label = DynamicValue.FromString("Blue"), Value = "blue" },
            ],
        };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<ComboBox>(control);
        var combo = (ComboBox)control;
        Assert.Equal(2, combo.Items.Count);
    }

    [AvaloniaFact]
    public void Create_MutuallyExclusive_PreSelectsCurrentValue()
    {
        var (dm, ctx) = Setup();
        var entry = new ChoicePickerCatalogEntry();
        var component = new ChoicePickerComponent
        {
            Id = "cp2",
            Options =
            [
                new ChoiceOption { Label = DynamicValue.FromString("Red"), Value = "red" },
                new ChoiceOption { Label = DynamicValue.FromString("Blue"), Value = "blue" },
                new ChoiceOption { Label = DynamicValue.FromString("Green"), Value = "green" },
            ],
            Value = DynamicValue.FromString("blue"),
        };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<ComboBox>(control);
        var combo = (ComboBox)control;
        Assert.Equal(1, combo.SelectedIndex);
    }

    [AvaloniaFact]
    public void Create_Filterable_CreatesAutoCompleteBox()
    {
        var (dm, ctx) = Setup();
        var entry = new ChoicePickerCatalogEntry();
        var component = new ChoicePickerComponent
        {
            Id = "cp3",
            Filterable = true,
            Options =
            [
                new ChoiceOption { Label = DynamicValue.FromString("Red"), Value = "red" },
                new ChoiceOption { Label = DynamicValue.FromString("Blue"), Value = "blue" },
            ],
        };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<AutoCompleteBox>(control);
    }

    [AvaloniaFact]
    public void Create_MultipleSelection_CreatesListBox()
    {
        var (dm, ctx) = Setup();
        var entry = new ChoicePickerCatalogEntry();
        var component = new ChoicePickerComponent
        {
            Id = "cp4",
            Variant = "multipleSelection",
            Options =
            [
                new ChoiceOption { Label = DynamicValue.FromString("Red"), Value = "red" },
                new ChoiceOption { Label = DynamicValue.FromString("Blue"), Value = "blue" },
            ],
        };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<ListBox>(control);
        var listBox = (ListBox)control;
        Assert.Equal(2, listBox.Items.Count);

        // Each item should be a ListBoxItem with a CheckBox as Content
        var firstItem = listBox.Items[0] as ListBoxItem;
        Assert.NotNull(firstItem);
        Assert.IsType<CheckBox>(firstItem.Content);
    }

    [AvaloniaFact]
    public void Create_MultipleSelection_PreSelected_DoesNotFirePhantomValueChanged()
    {
        // Arrange — pre-load the bound path with two selected values.
        var dm = new DataModel();
        dm.Apply(
            new UpdateDataModel
            {
                SurfaceId = "test",
                Path = "/selected",
                Value = System.Text.Json.JsonSerializer.SerializeToElement(new[] { "red", "blue" }),
            }
        );
        var ctx = new MockRenderContext(dm);
        var entry = new ChoicePickerCatalogEntry();
        var component = new ChoicePickerComponent
        {
            Id = "cp_phantom",
            Variant = "multipleSelection",
            Value = DynamicValue.FromPath("/selected"),
            Options =
            [
                new ChoiceOption { Label = DynamicValue.FromString("Red"), Value = "red" },
                new ChoiceOption { Label = DynamicValue.FromString("Blue"), Value = "blue" },
                new ChoiceOption { Label = DynamicValue.FromString("Green"), Value = "green" },
            ],
        };

        // Act
        var control = entry.Create(component, dm, ctx);

        // Assert — the initial selection application must not synthesize user actions upstream.
        Assert.IsType<ListBox>(control);
        var listBox = (ListBox)control;
        Assert.Equal(2, listBox.SelectedItems!.Count);
        Assert.Empty(ctx.FiredActions);
    }

    [AvaloniaFact]
    public void Create_MultipleSelection_Chips_CreatesWrapPanel()
    {
        var (dm, ctx) = Setup();
        var entry = new ChoicePickerCatalogEntry();
        var component = new ChoicePickerComponent
        {
            Id = "cp5",
            Variant = "multipleSelection",
            DisplayStyle = "chips",
            Options =
            [
                new ChoiceOption { Label = DynamicValue.FromString("Red"), Value = "red" },
                new ChoiceOption { Label = DynamicValue.FromString("Blue"), Value = "blue" },
                new ChoiceOption { Label = DynamicValue.FromString("Green"), Value = "green" },
            ],
        };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<WrapPanel>(control);
        var panel = (WrapPanel)control;
        Assert.Equal(3, panel.Children.Count);
        Assert.All(panel.Children, child => Assert.IsType<ToggleButton>(child));
        Assert.Equal(Orientation.Horizontal, panel.Orientation);
    }
}
