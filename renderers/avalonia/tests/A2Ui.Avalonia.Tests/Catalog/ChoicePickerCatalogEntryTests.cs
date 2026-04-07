using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using FluentAssertions;

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
        var component = new A2UiComponent
        {
            Id = "cp1",
            Component = "ChoicePicker",
            Options =
            [
                new ChoiceOption { Label = "Red", Value = "red" },
                new ChoiceOption { Label = "Blue", Value = "blue" },
            ],
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<ComboBox>();
        var combo = (ComboBox)control;
        combo.Items.Should().HaveCount(2);
    }

    [AvaloniaFact]
    public void Create_MutuallyExclusive_PreSelectsCurrentValue()
    {
        var (dm, ctx) = Setup();
        var entry = new ChoicePickerCatalogEntry();
        var component = new A2UiComponent
        {
            Id = "cp2",
            Component = "ChoicePicker",
            Options =
            [
                new ChoiceOption { Label = "Red", Value = "red" },
                new ChoiceOption { Label = "Blue", Value = "blue" },
                new ChoiceOption { Label = "Green", Value = "green" },
            ],
            Value = DynamicValue.FromString("blue"),
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<ComboBox>();
        var combo = (ComboBox)control;
        combo.SelectedIndex.Should().Be(1, "because 'blue' is the second option (index 1)");
    }

    [AvaloniaFact]
    public void Create_Filterable_CreatesAutoCompleteBox()
    {
        var (dm, ctx) = Setup();
        var entry = new ChoicePickerCatalogEntry();
        var component = new A2UiComponent
        {
            Id = "cp3",
            Component = "ChoicePicker",
            Filterable = true,
            Options =
            [
                new ChoiceOption { Label = "Red", Value = "red" },
                new ChoiceOption { Label = "Blue", Value = "blue" },
            ],
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<AutoCompleteBox>();
    }

    [AvaloniaFact]
    public void Create_MultipleSelection_CreatesListBox()
    {
        var (dm, ctx) = Setup();
        var entry = new ChoicePickerCatalogEntry();
        var component = new A2UiComponent
        {
            Id = "cp4",
            Component = "ChoicePicker",
            Variant = "multipleSelection",
            Options =
            [
                new ChoiceOption { Label = "Red", Value = "red" },
                new ChoiceOption { Label = "Blue", Value = "blue" },
            ],
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<ListBox>();
        var listBox = (ListBox)control;
        listBox.Items.Should().HaveCount(2);

        // Each item should be a ListBoxItem with a CheckBox as Content
        var firstItem = listBox.Items[0] as ListBoxItem;
        firstItem.Should().NotBeNull();
        firstItem!.Content.Should().BeOfType<CheckBox>();
    }

    [AvaloniaFact]
    public void Create_MultipleSelection_Chips_CreatesWrapPanel()
    {
        var (dm, ctx) = Setup();
        var entry = new ChoicePickerCatalogEntry();
        var component = new A2UiComponent
        {
            Id = "cp5",
            Component = "ChoicePicker",
            Variant = "multipleSelection",
            DisplayStyle = "chips",
            Options =
            [
                new ChoiceOption { Label = "Red", Value = "red" },
                new ChoiceOption { Label = "Blue", Value = "blue" },
                new ChoiceOption { Label = "Green", Value = "green" },
            ],
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<WrapPanel>();
        var panel = (WrapPanel)control;
        panel.Children.Should().HaveCount(3);
        panel.Children.Should().AllBeOfType<ToggleButton>();
        panel.Orientation.Should().Be(Orientation.Horizontal);
    }
}
