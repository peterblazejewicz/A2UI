using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class DateTimeInputCatalogEntryTests
{
    [AvaloniaFact]
    public void Create_DateOnly_Default_ReturnsCalendarDatePicker()
    {
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);

        var component = new A2UiComponent
        {
            Id = "dt1",
            Component = "DateTimeInput",
            Label = DynamicValue.FromString("Pick a date"),
        };

        var control = entry.Create(component, dm, ctx);
        control.Should().BeOfType<CalendarDatePicker>();
    }

    [AvaloniaFact]
    public void Create_DateOnly_ExplicitFlags_ReturnsCalendarDatePicker()
    {
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);

        var component = new A2UiComponent
        {
            Id = "dt2",
            Component = "DateTimeInput",
            EnableDate = true,
            EnableTime = false,
        };

        var control = entry.Create(component, dm, ctx);
        control.Should().BeOfType<CalendarDatePicker>();
    }

    [AvaloniaFact]
    public void Create_DateAndTime_ReturnsStackPanelWithBothPickers()
    {
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);

        var component = new A2UiComponent
        {
            Id = "dt3",
            Component = "DateTimeInput",
            EnableDate = true,
            EnableTime = true,
        };

        var control = entry.Create(component, dm, ctx);
        control.Should().BeOfType<StackPanel>();

        var panel = (StackPanel)control;
        panel.Children.OfType<CalendarDatePicker>().Should().ContainSingle();
        panel.Children.OfType<TimePicker>().Should().ContainSingle();
    }

    [AvaloniaFact]
    public void Create_TimeOnly_ReturnsTimePicker()
    {
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);

        var component = new A2UiComponent
        {
            Id = "dt4",
            Component = "DateTimeInput",
            EnableDate = false,
            EnableTime = true,
        };

        var control = entry.Create(component, dm, ctx);
        control.Should().BeOfType<TimePicker>();
    }

    [AvaloniaFact]
    public void Create_DateAndTime_WithInitialValue_SetsBothControls()
    {
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);

        var component = new A2UiComponent
        {
            Id = "dt5",
            Component = "DateTimeInput",
            EnableDate = true,
            EnableTime = true,
            Value = DynamicValue.FromString("2026-04-10T19:30:00"),
        };

        var control = entry.Create(component, dm, ctx);
        var panel = (StackPanel)control;
        var datePicker = panel.Children.OfType<CalendarDatePicker>().Single();
        var timePicker = panel.Children.OfType<TimePicker>().Single();

        datePicker.SelectedDate.Should().NotBeNull();
        datePicker.SelectedDate!.Value.Date.Should().Be(new DateTime(2026, 4, 10));
        timePicker.SelectedTime.Should().Be(new TimeSpan(19, 30, 0));
    }

    [AvaloniaFact]
    public void Update_DateOnly_UpdatesSelectedDate()
    {
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);

        var comp = new A2UiComponent
        {
            Id = "dt6",
            Component = "DateTimeInput",
            Value = DynamicValue.FromString("2026-01-01"),
        };

        var control = entry.Create(comp, dm, ctx);

        var comp2 = new A2UiComponent
        {
            Id = "dt6",
            Component = "DateTimeInput",
            Value = DynamicValue.FromString("2026-12-25"),
        };

        bool updated = entry.Update(control, comp2, dm, ctx);
        updated.Should().BeTrue();

        var picker = (CalendarDatePicker)control;
        picker.SelectedDate!.Value.Date.Should().Be(new DateTime(2026, 12, 25));
    }
}
