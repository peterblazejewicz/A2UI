using A2Ui.Avalonia.Catalog;
using A2Ui.Avalonia.Catalog.Entries;
using A2Ui.Core;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class DateTimeInputCatalogEntryTests
{
    [AvaloniaFact]
    public void Create_DateOnly_Default_ReturnsCalendarDatePicker()
    {
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);

        var component = new DateTimeInputComponent { Id = "dt1", Label = DynamicValue.FromString("Pick a date") };

        var control = entry.Create(component, dm, ctx);
        Assert.IsType<CalendarDatePicker>(control);
    }

    [AvaloniaFact]
    public void Create_DateOnly_ExplicitFlags_ReturnsCalendarDatePicker()
    {
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);

        var component = new DateTimeInputComponent
        {
            Id = "dt2",
            EnableDate = true,
            EnableTime = false,
        };

        var control = entry.Create(component, dm, ctx);
        Assert.IsType<CalendarDatePicker>(control);
    }

    [AvaloniaFact]
    public void Create_DateAndTime_ReturnsStackPanelWithBothPickers()
    {
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);

        var component = new DateTimeInputComponent
        {
            Id = "dt3",
            EnableDate = true,
            EnableTime = true,
        };

        var control = entry.Create(component, dm, ctx);
        Assert.IsType<StackPanel>(control);

        var panel = (StackPanel)control;
        Assert.Single(panel.Children.OfType<CalendarDatePicker>());
        Assert.Single(panel.Children.OfType<TimePicker>());
    }

    [AvaloniaFact]
    public void Create_TimeOnly_ReturnsTimePicker()
    {
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);

        var component = new DateTimeInputComponent
        {
            Id = "dt4",
            EnableDate = false,
            EnableTime = true,
        };

        var control = entry.Create(component, dm, ctx);
        Assert.IsType<TimePicker>(control);
    }

    [AvaloniaFact]
    public void Create_DateAndTime_WithInitialValue_SetsBothControls()
    {
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);

        var component = new DateTimeInputComponent
        {
            Id = "dt5",
            EnableDate = true,
            EnableTime = true,
            Value = DynamicValue.FromString("2026-04-10T19:30:00"),
        };

        var control = entry.Create(component, dm, ctx);
        var panel = (StackPanel)control;
        var datePicker = panel.Children.OfType<CalendarDatePicker>().Single();
        var timePicker = panel.Children.OfType<TimePicker>().Single();

        Assert.NotNull(datePicker.SelectedDate);
        Assert.Equal(new DateTime(2026, 4, 10), datePicker.SelectedDate!.Value.Date);
        Assert.Equal(new TimeSpan(19, 30, 0), timePicker.SelectedTime);
    }

    [AvaloniaFact]
    public void Update_DateOnly_UpdatesSelectedDate()
    {
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);

        var comp = new DateTimeInputComponent { Id = "dt6", Value = DynamicValue.FromString("2026-01-01") };

        var control = entry.Create(comp, dm, ctx);

        var comp2 = new DateTimeInputComponent { Id = "dt6", Value = DynamicValue.FromString("2026-12-25") };

        bool updated = entry.Update(control, comp2, dm, ctx);
        Assert.True(updated);

        var picker = (CalendarDatePicker)control;
        Assert.Equal(new DateTime(2026, 12, 25), picker.SelectedDate!.Value.Date);
    }
}
