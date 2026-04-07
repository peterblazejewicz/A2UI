using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class InputWriteBackTests
{
    private static (DataModel dm, MockRenderContext ctx) Setup()
    {
        var dm = new DataModel();
        return (dm, new MockRenderContext(dm));
    }

    // ── TextField write-back ──────────────────────────────────────────────

    [AvaloniaFact]
    public void TextField_ValueChange_WritesBackToDataModel()
    {
        var (dm, ctx) = Setup();
        var entry = new TextFieldCatalogEntry();
        var component = new A2UiComponent
        {
            Id = "tf1",
            Component = "TextField",
            Label = DynamicValue.FromString("Name"),
            Value = DynamicValue.FromPath("/name"),
        };

        var control = entry.Create(component, dm, ctx);
        TextBox? tb = CheckHelper.FindInner<TextBox>(control);
        tb.Should().NotBeNull();

        tb!.Text = "Alice";

        ctx.DataModelUpdates.Should().Contain(("/name", "Alice"));
    }

    [AvaloniaFact]
    public void TextField_ValueChange_FiresActionWithComponentId()
    {
        var (dm, ctx) = Setup();
        var entry = new TextFieldCatalogEntry();
        var component = new A2UiComponent
        {
            Id = "tf2",
            Component = "TextField",
            Label = DynamicValue.FromString("Email"),
            Value = DynamicValue.FromPath("/email"),
        };

        var control = entry.Create(component, dm, ctx);
        TextBox? tb = CheckHelper.FindInner<TextBox>(control);
        tb.Should().NotBeNull();

        tb!.Text = "alice@example.com";

        ctx.FiredActions.Should().ContainSingle(a => a.ComponentId == "tf2");
        ctx.FiredActions[0].EventName.Should().Be("valueChanged");
        ctx.FiredActions[0].Payload.Should().Be("alice@example.com");
    }

    // ── Slider write-back ─────────────────────────────────────────────────

    [AvaloniaFact]
    public void Slider_Create_WithBoundPath_HasCorrectValue()
    {
        var (dm, ctx) = Setup();
        var entry = new SliderCatalogEntry();
        var component = new A2UiComponent
        {
            Id = "s1",
            Component = "Slider",
            Value = DynamicValue.FromNumber(75),
            Min = DynamicValue.FromNumber(0),
            Max = DynamicValue.FromNumber(100),
        };

        var control = entry.Create(component, dm, ctx);

        control.Should().BeOfType<Slider>();
        var slider = (Slider)control;
        slider.Value.Should().Be(75);
        slider.Minimum.Should().Be(0);
        slider.Maximum.Should().Be(100);
    }

    // ── DateTimeInput write-back ──────────────────────────────────────────

    [AvaloniaFact]
    public void DateTimeInput_Create_WithDateValue_ParsesDate()
    {
        var (dm, ctx) = Setup();
        var entry = new DateTimeInputCatalogEntry();
        var component = new A2UiComponent
        {
            Id = "dt1",
            Component = "DateTimeInput",
            Label = DynamicValue.FromString("Birthday"),
            Value = DynamicValue.FromString("2024-06-15"),
        };

        var control = entry.Create(component, dm, ctx);

        var picker = CheckHelper.FindInner<CalendarDatePicker>(control);
        picker.Should().NotBeNull();
        picker!.SelectedDate.Should().NotBeNull();
        picker.SelectedDate!.Value.Year.Should().Be(2024);
        picker.SelectedDate.Value.Month.Should().Be(6);
        picker.SelectedDate.Value.Day.Should().Be(15);
    }

    [AvaloniaFact]
    public void DateTimeInput_Create_WithBoundPath_SetsWatermark()
    {
        var (dm, ctx) = Setup();
        var entry = new DateTimeInputCatalogEntry();
        var component = new A2UiComponent
        {
            Id = "dt2",
            Component = "DateTimeInput",
            Label = DynamicValue.FromString("Start date"),
            Value = DynamicValue.FromPath("/startDate"),
        };

        var control = entry.Create(component, dm, ctx);

        var picker = CheckHelper.FindInner<CalendarDatePicker>(control);
        picker.Should().NotBeNull();
        picker!.Watermark.Should().Be("Start date");
        // No initial date value in data model, so SelectedDate should be null
        picker.SelectedDate.Should().BeNull();
    }
}
