using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class NewCatalogEntryTests
{
    private static (DataModel dm, MockRenderContext ctx) Setup()
    {
        var dm = new DataModel();
        return (dm, new MockRenderContext(dm));
    }

    // ── Icon ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void IconCatalogEntry_Create_RendersKnownIcon()
    {
        var (dm, ctx) = Setup();
        var entry = new IconCatalogEntry();
        var c = new A2UiComponent
        {
            Id = "i1", Component = "Icon",
            Name = DynamicValue.FromString("send"),
        };

        var control = entry.Create(c, dm, ctx);

        control.Should().BeOfType<TextBlock>();
        ((TextBlock)control).Text.Should().Be("\u27A4");
    }

    [AvaloniaFact]
    public void IconCatalogEntry_Create_UnknownIcon_ShowsDefault()
    {
        var (dm, ctx) = Setup();
        var entry = new IconCatalogEntry();
        var c = new A2UiComponent
        {
            Id = "i2", Component = "Icon",
            Name = DynamicValue.FromString("unknownIcon"),
        };

        var control = entry.Create(c, dm, ctx);

        ((TextBlock)control).Text.Should().Be("\u25A0");
    }

    // ── Divider ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void DividerCatalogEntry_Create_ReturnsSeparator()
    {
        var (dm, ctx) = Setup();
        var entry = new DividerCatalogEntry();
        var c = new A2UiComponent { Id = "d1", Component = "Divider" };

        var control = entry.Create(c, dm, ctx);

        control.Should().BeOfType<Separator>();
    }

    // ── Video ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void VideoCatalogEntry_Create_ShowsPlaceholder()
    {
        var (dm, ctx) = Setup();
        var entry = new VideoCatalogEntry();
        var c = new A2UiComponent
        {
            Id = "v1", Component = "Video",
            Url = DynamicValue.FromString("https://example.com/video.mp4"),
        };

        var control = entry.Create(c, dm, ctx);

        control.Should().BeOfType<TextBlock>();
        ((TextBlock)control).Text.Should().Contain("Video");
        ((TextBlock)control).Text.Should().Contain("video.mp4");
    }

    // ── AudioPlayer ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void AudioPlayerCatalogEntry_Create_ShowsPlaceholder()
    {
        var (dm, ctx) = Setup();
        var entry = new AudioPlayerCatalogEntry();
        var c = new A2UiComponent
        {
            Id = "a1", Component = "AudioPlayer",
            Url = DynamicValue.FromString("https://example.com/audio.mp3"),
        };

        var control = entry.Create(c, dm, ctx);

        control.Should().BeOfType<TextBlock>();
        ((TextBlock)control).Text.Should().Contain("AudioPlayer");
    }

    // ── List ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void ListCatalogEntry_Create_ReturnsScrollViewer()
    {
        var (dm, ctx) = Setup();
        var entry = new ListCatalogEntry();
        var c = new A2UiComponent { Id = "l1", Component = "List" };

        var control = entry.Create(c, dm, ctx);

        control.Should().BeOfType<ScrollViewer>();
    }

    // ── Tabs ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void TabsCatalogEntry_Create_ReturnsTabControl()
    {
        var (dm, ctx) = Setup();
        var entry = new TabsCatalogEntry();
        var c = new A2UiComponent
        {
            Id = "tabs1", Component = "Tabs",
            Tabs =
            [
                new TabDefinition { Title = "Tab 1", Child = "p1" },
                new TabDefinition { Title = "Tab 2", Child = "p2" },
            ]
        };

        var control = entry.Create(c, dm, ctx);

        control.Should().BeOfType<TabControl>();
        ((TabControl)control).Items.Should().HaveCount(2);
    }

    // ── Modal ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void ModalCatalogEntry_Create_ReturnsPanel()
    {
        var (dm, ctx) = Setup();
        var entry = new ModalCatalogEntry();
        var c = new A2UiComponent
        {
            Id = "m1", Component = "Modal",
            Trigger = "btn1",
            Content = "panel1",
        };

        var control = entry.Create(c, dm, ctx);

        // Modal now returns a Panel containing the trigger + a Popup overlay
        control.Should().BeOfType<Panel>();
    }

    // ── ChoicePicker ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void ChoicePickerCatalogEntry_Create_PopulatesOptions()
    {
        var (dm, ctx) = Setup();
        var entry = new ChoicePickerCatalogEntry();
        var c = new A2UiComponent
        {
            Id = "cp1", Component = "ChoicePicker",
            Options =
            [
                new ChoiceOption { Label = "Red", Value = "red" },
                new ChoiceOption { Label = "Blue", Value = "blue" },
            ]
        };

        var control = entry.Create(c, dm, ctx);

        control.Should().BeOfType<ComboBox>();
        ((ComboBox)control).Items.Should().HaveCount(2);
    }

    // ── CheckBox (renamed from Checkbox) ──────────────────────────────────

    [AvaloniaFact]
    public void CheckBoxCatalogEntry_Create_SetsLabel()
    {
        var (dm, ctx) = Setup();
        var entry = new CheckBoxCatalogEntry();
        var c = new A2UiComponent
        {
            Id = "cb1", Component = "CheckBox",
            Label = DynamicValue.FromString("Accept terms"),
            Value = DynamicValue.FromBool(true),
        };

        var control = entry.Create(c, dm, ctx);

        control.Should().BeOfType<CheckBox>();
        var cb = (CheckBox)control;
        cb.Content.Should().Be("Accept terms");
        cb.IsChecked.Should().BeTrue();
    }

    // ── Slider with min/max ───────────────────────────────────────────────

    [AvaloniaFact]
    public void SliderCatalogEntry_Create_RespectsMinMax()
    {
        var (dm, ctx) = Setup();
        var entry = new SliderCatalogEntry();
        var c = new A2UiComponent
        {
            Id = "s1", Component = "Slider",
            Value = DynamicValue.FromNumber(50),
            Min = DynamicValue.FromNumber(10),
            Max = DynamicValue.FromNumber(200),
        };

        var control = entry.Create(c, dm, ctx);

        control.Should().BeOfType<Slider>();
        var slider = (Slider)control;
        slider.Minimum.Should().Be(10);
        slider.Maximum.Should().Be(200);
    }
}
