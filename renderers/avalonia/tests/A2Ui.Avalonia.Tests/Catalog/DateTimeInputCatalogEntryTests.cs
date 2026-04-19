using A2Ui.Avalonia.Catalog;
using A2Ui.Avalonia.Catalog.Entries;
using A2Ui.Core;
using A2Ui.Core.Bindings;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;
using A2Ui.Core.Surfaces;
using A2Ui.Core.Validation;
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

    [AvaloniaFact]
    public void Update_DateAndTime_WithChecks_UpdatesInPlace()
    {
        // Arrange — composite DateTimeInput wrapped in a check panel.
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);
        CheckRule[] checks = [new CheckRule { Condition = DynamicValue.FromBool(true), Message = "Required" }];
        var comp = new DateTimeInputComponent
        {
            Id = "dt_composite_checks",
            EnableDate = true,
            EnableTime = true,
            Value = DynamicValue.FromString("2026-01-01T08:00:00"),
            Checks = checks,
        };

        var control = entry.Create(comp, dm, ctx);
        // Create wraps the tagged composite panel inside an outer check-wrapper StackPanel.
        Assert.IsType<StackPanel>(control);
        var outerWrapper = (StackPanel)control;
        var innerPanel = (StackPanel)outerWrapper.Children[0];
        var datePicker = innerPanel.Children.OfType<CalendarDatePicker>().Single();
        var timePicker = innerPanel.Children.OfType<TimePicker>().Single();

        // Act — update to a new date/time value.
        var comp2 = new DateTimeInputComponent
        {
            Id = "dt_composite_checks",
            EnableDate = true,
            EnableTime = true,
            Value = DynamicValue.FromString("2026-12-25T19:30:00"),
            Checks = checks,
        };
        bool updated = entry.Update(control, comp2, dm, ctx);

        // Assert — Update must hit the composite branch (return true) and mutate the
        // existing inner pickers rather than fall through, which would force the caller
        // to rebuild the control and discard focus / in-flight picker state.
        Assert.True(updated);
        Assert.Equal(new DateTime(2026, 12, 25), datePicker.SelectedDate!.Value.Date);
        Assert.Equal(new TimeSpan(19, 30, 0), timePicker.SelectedTime);
    }

    [AvaloniaFact]
    public void Update_DateAndTime_WithFailingCheck_RefreshesErrorMessage()
    {
        // Arrange — composite DateTimeInput with a failing check on initial render.
        var entry = new DateTimeInputCatalogEntry();
        var dm = new DataModel();
        var ctx = new DataModelCapturingRenderContext(dm);
        var comp = new DateTimeInputComponent
        {
            Id = "dt_composite_errors",
            EnableDate = true,
            EnableTime = true,
            Checks = [new CheckRule { Condition = DynamicValue.FromBool(false), Message = "Initial error" }],
        };

        var control = entry.Create(comp, dm, ctx);
        var outerWrapper = (StackPanel)control;
        // Create renders the initial error TextBlock beneath the inner composite panel.
        Assert.Equal("Initial error", outerWrapper.Children.OfType<TextBlock>().Single().Text);

        // Act — update swaps the check message. In-place Update must refresh the TextBlock.
        var comp2 = new DateTimeInputComponent
        {
            Id = "dt_composite_errors",
            EnableDate = true,
            EnableTime = true,
            Checks = [new CheckRule { Condition = DynamicValue.FromBool(false), Message = "Updated error" }],
        };
        bool updated = entry.Update(control, comp2, dm, ctx);

        // Assert — Update returned true and the visible error message reflects the new check.
        Assert.True(updated);
        Assert.Equal("Updated error", outerWrapper.Children.OfType<TextBlock>().Single().Text);
    }
}
