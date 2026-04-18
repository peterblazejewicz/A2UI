using A2Ui.Avalonia.Catalog;
using A2Ui.Avalonia.Catalog.Entries;
using A2Ui.Core;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;
using A2Ui.Core.Surfaces;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Xunit;

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
        var c = new IconComponent { Id = "i1", Name = DynamicValue.FromString("send") };

        var control = entry.Create(c, dm, ctx);

        Assert.IsType<TextBlock>(control);
        Assert.Equal("\u27A4", ((TextBlock)control).Text);
    }

    [AvaloniaFact]
    public void IconCatalogEntry_Create_UnknownIcon_ShowsDefault()
    {
        var (dm, ctx) = Setup();
        var entry = new IconCatalogEntry();
        var c = new IconComponent { Id = "i2", Name = DynamicValue.FromString("unknownIcon") };

        var control = entry.Create(c, dm, ctx);

        Assert.Equal("\u25A0", ((TextBlock)control).Text);
    }

    // ── Divider ───────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void DividerCatalogEntry_Create_ReturnsSeparator()
    {
        var (dm, ctx) = Setup();
        var entry = new DividerCatalogEntry();
        var c = new DividerComponent { Id = "d1" };

        var control = entry.Create(c, dm, ctx);

        Assert.IsType<Separator>(control);
    }

    [AvaloniaFact]
    public void DividerCatalogEntry_Create_VerticalAxis_SetsWidthAndAlignment()
    {
        var (dm, ctx) = Setup();
        var entry = new DividerCatalogEntry();
        var c = new DividerComponent { Id = "d2", Axis = "vertical" };

        var control = entry.Create(c, dm, ctx);

        Assert.IsType<Separator>(control);
        var sep = (Separator)control;
        Assert.Equal(1, sep.Width);
        Assert.Equal(VerticalAlignment.Stretch, sep.VerticalAlignment);
        Assert.Equal(HorizontalAlignment.Center, sep.HorizontalAlignment);
    }

    // ── Video ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void VideoCatalogEntry_Create_ShowsPlaceholder()
    {
        var (dm, ctx) = Setup();
        var entry = new VideoCatalogEntry();
        var c = new VideoComponent { Id = "v1", Url = DynamicValue.FromString("https://example.com/video.mp4") };

        var control = entry.Create(c, dm, ctx);

        Assert.IsType<TextBlock>(control);
        Assert.Contains("Video", ((TextBlock)control).Text);
        Assert.Contains("video.mp4", ((TextBlock)control).Text);
    }

    // ── AudioPlayer ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void AudioPlayerCatalogEntry_Create_ShowsPlaceholder()
    {
        var (dm, ctx) = Setup();
        var entry = new AudioPlayerCatalogEntry();
        var c = new AudioPlayerComponent { Id = "a1", Url = DynamicValue.FromString("https://example.com/audio.mp3") };

        var control = entry.Create(c, dm, ctx);

        Assert.IsType<TextBlock>(control);
        Assert.Contains("AudioPlayer", ((TextBlock)control).Text);
    }

    // ── List ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void ListCatalogEntry_Create_ReturnsScrollViewer()
    {
        var (dm, ctx) = Setup();
        var entry = new ListCatalogEntry();
        var c = new ListComponent { Id = "l1" };

        var control = entry.Create(c, dm, ctx);

        Assert.IsType<ScrollViewer>(control);
    }

    // ── Tabs ──────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void TabsCatalogEntry_Create_ReturnsTabControl()
    {
        var (dm, ctx) = Setup();
        var entry = new TabsCatalogEntry();
        var c = new TabsComponent
        {
            Id = "tabs1",
            Tabs =
            [
                new TabDefinition { Title = "Tab 1", Child = "p1" },
                new TabDefinition { Title = "Tab 2", Child = "p2" },
            ],
        };

        var control = entry.Create(c, dm, ctx);

        Assert.IsType<TabControl>(control);
        Assert.Equal(2, ((TabControl)control).Items.Count);
    }

    // ── Modal ─────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void ModalCatalogEntry_Create_ReturnsPanel()
    {
        var (dm, ctx) = Setup();
        var entry = new ModalCatalogEntry();
        var c = new ModalComponent
        {
            Id = "m1",
            Trigger = "btn1",
            Content = "panel1",
        };

        var control = entry.Create(c, dm, ctx);

        // Modal now returns a Panel containing the trigger + a Popup overlay
        Assert.IsType<Panel>(control);
    }

    // ── ChoicePicker ──────────────────────────────────────────────────────

    [AvaloniaFact]
    public void ChoicePickerCatalogEntry_Create_PopulatesOptions()
    {
        var (dm, ctx) = Setup();
        var entry = new ChoicePickerCatalogEntry();
        var c = new ChoicePickerComponent
        {
            Id = "cp1",
            Options =
            [
                new ChoiceOption { Label = "Red", Value = "red" },
                new ChoiceOption { Label = "Blue", Value = "blue" },
            ],
        };

        var control = entry.Create(c, dm, ctx);

        Assert.IsType<ComboBox>(control);
        Assert.Equal(2, ((ComboBox)control).Items.Count);
    }

    // ── CheckBox (renamed from Checkbox) ──────────────────────────────────

    [AvaloniaFact]
    public void CheckBoxCatalogEntry_Create_SetsLabel()
    {
        var (dm, ctx) = Setup();
        var entry = new CheckBoxCatalogEntry();
        var c = new CheckBoxComponent
        {
            Id = "cb1",
            Label = DynamicValue.FromString("Accept terms"),
            Value = DynamicValue.FromBool(true),
        };

        var control = entry.Create(c, dm, ctx);

        Assert.IsType<CheckBox>(control);
        var cb = (CheckBox)control;
        Assert.Equal("Accept terms", cb.Content);
        Assert.True(cb.IsChecked);
    }

    [AvaloniaFact]
    public void CheckBoxCatalogEntry_Toggle_WritesDataModelAndFiresActionWithComponentId()
    {
        // Arrange
        var entry = new CheckBoxCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);
        var c = new CheckBoxComponent
        {
            Id = "cb_bound",
            Label = DynamicValue.FromString("Agree"),
            Value = DynamicValue.FromPath("/agree"),
        };

        // Act
        var control = entry.Create(c, dm, ctx);
        var cb = (CheckBox)control;
        cb.IsChecked = true;

        // Assert — both the local data model and the upstream action must reflect the toggle.
        Assert.Equal("true", dm.Resolve(DynamicValue.FromPath("/agree")));
        Assert.Single(ctx.FiredActions, a => a.ComponentId == "cb_bound");
        Assert.Equal("true", ctx.FiredActions[0].Payload);
    }

    // ── Slider with min/max ───────────────────────────────────────────────

    [AvaloniaFact]
    public void SliderCatalogEntry_Create_RespectsMinMax()
    {
        var (dm, ctx) = Setup();
        var entry = new SliderCatalogEntry();
        var c = new SliderComponent
        {
            Id = "s1",
            Value = DynamicValue.FromNumber(50),
            Min = DynamicValue.FromNumber(10),
            Max = DynamicValue.FromNumber(200),
        };

        var control = entry.Create(c, dm, ctx);

        Assert.IsType<Slider>(control);
        var slider = (Slider)control;
        Assert.Equal(10, slider.Minimum);
        Assert.Equal(200, slider.Maximum);
    }
}
