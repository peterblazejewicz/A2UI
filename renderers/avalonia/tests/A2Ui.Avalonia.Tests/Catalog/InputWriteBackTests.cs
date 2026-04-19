using A2Ui.Avalonia.Catalog.Entries;
using A2Ui.Core.Bindings;
using A2Ui.Core.Components;
using A2Ui.Core.Surfaces;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

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
        var component = new TextFieldComponent
        {
            Id = "tf1",
            Label = DynamicValue.FromString("Name"),
            Value = DynamicValue.FromPath("/name"),
        };

        var control = entry.Create(component, dm, ctx);
        TextBox? tb = CheckHelper.FindInner<TextBox>(control);
        Assert.NotNull(tb);

        tb.Text = "Alice";

        Assert.Contains(("/name", "Alice"), ctx.DataModelUpdates);
    }

    [AvaloniaFact]
    public void TextField_ValueChange_FiresActionWithComponentId()
    {
        var (dm, ctx) = Setup();
        var entry = new TextFieldCatalogEntry();
        var component = new TextFieldComponent
        {
            Id = "tf2",
            Label = DynamicValue.FromString("Email"),
            Value = DynamicValue.FromPath("/email"),
        };

        var control = entry.Create(component, dm, ctx);
        TextBox? tb = CheckHelper.FindInner<TextBox>(control);
        Assert.NotNull(tb);

        tb.Text = "alice@example.com";

        Assert.Single(ctx.FiredActions, a => a.ComponentId == "tf2");
        Assert.Equal("valueChanged", ctx.FiredActions[0].EventName);
        Assert.Equal("alice@example.com", ctx.FiredActions[0].Payload);
    }

    // ── Slider write-back ─────────────────────────────────────────────────

    [AvaloniaFact]
    public void Slider_Create_WithBoundPath_HasCorrectValue()
    {
        var (dm, ctx) = Setup();
        var entry = new SliderCatalogEntry();
        var component = new SliderComponent
        {
            Id = "s1",
            Value = DynamicValue.FromNumber(75),
            Min = DynamicValue.FromNumber(0),
            Max = DynamicValue.FromNumber(100),
        };

        var control = entry.Create(component, dm, ctx);

        Assert.IsType<Slider>(control);
        var slider = (Slider)control;
        Assert.Equal(75, slider.Value);
        Assert.Equal(0, slider.Minimum);
        Assert.Equal(100, slider.Maximum);
    }

    // ── DateTimeInput write-back ──────────────────────────────────────────

    [AvaloniaFact]
    public void DateTimeInput_Create_WithDateValue_ParsesDate()
    {
        var (dm, ctx) = Setup();
        var entry = new DateTimeInputCatalogEntry();
        var component = new DateTimeInputComponent
        {
            Id = "dt1",
            Label = DynamicValue.FromString("Birthday"),
            Value = DynamicValue.FromString("2024-06-15"),
        };

        var control = entry.Create(component, dm, ctx);

        var picker = CheckHelper.FindInner<CalendarDatePicker>(control);
        Assert.NotNull(picker);
        Assert.NotNull(picker.SelectedDate);
        Assert.Equal(2024, picker.SelectedDate!.Value.Year);
        Assert.Equal(6, picker.SelectedDate.Value.Month);
        Assert.Equal(15, picker.SelectedDate.Value.Day);
    }

    [AvaloniaFact]
    public void DateTimeInput_Create_WithBoundPath_SetsPlaceholder()
    {
        var (dm, ctx) = Setup();
        var entry = new DateTimeInputCatalogEntry();
        var component = new DateTimeInputComponent
        {
            Id = "dt2",
            Label = DynamicValue.FromString("Start date"),
            Value = DynamicValue.FromPath("/startDate"),
        };

        var control = entry.Create(component, dm, ctx);

        var picker = CheckHelper.FindInner<CalendarDatePicker>(control);
        Assert.NotNull(picker);
        Assert.Equal("Start date", picker.PlaceholderText);
        // No initial date value in data model, so SelectedDate should be null
        Assert.Null(picker.SelectedDate);
    }
}
