using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;

namespace A2Ui.Avalonia.Catalog;

/// <summary>A2UI "TextField" → TextBox.</summary>
public sealed class TextFieldCatalogEntry : ICatalogEntry
{
    public string ComponentType => "TextField";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var tb = new TextBox
        {
            Text      = ctx.Resolve(c.Value) ?? string.Empty,
            Watermark = ctx.Resolve(c.Label),
        };
        return tb;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx)
    {
        if (existing is not TextBox tb) return false;
        if (!tb.IsFocused) tb.Text = ctx.Resolve(c.Value) ?? string.Empty;
        tb.Watermark = ctx.Resolve(c.Label);
        return true;
    }
}

/// <summary>A2UI "DateTimeInput" → CalendarDatePicker.</summary>
public sealed class DateTimeInputCatalogEntry : ICatalogEntry
{
    public string ComponentType => "DateTimeInput";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var picker = new CalendarDatePicker
        {
            Watermark = ctx.Resolve(c.Label) ?? "Select date",
        };

        var raw = ctx.Resolve(c.Value);
        if (raw is not null && DateOnly.TryParse(raw, out var date))
            picker.SelectedDate = date.ToDateTime(TimeOnly.MinValue);

        return picker;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "ChoicePicker" → ComboBox with options.</summary>
public sealed class ChoicePickerCatalogEntry : ICatalogEntry
{
    public string ComponentType => "ChoicePicker";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var combo = new ComboBox
        {
            PlaceholderText = ctx.Resolve(c.Label),
        };

        if (c.Options is { } opts)
        {
            foreach (var opt in opts)
                combo.Items.Add(new ComboBoxItem { Content = opt.Label, Tag = opt.Value });
        }

        return combo;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "CheckBox" → CheckBox control.</summary>
public sealed class CheckBoxCatalogEntry : ICatalogEntry
{
    public string ComponentType => "CheckBox";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        bool isChecked = ctx.Resolve(c.Value) is "true";
        return new CheckBox
        {
            Content   = ctx.Resolve(c.Label),
            IsChecked = isChecked,
        };
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx)
    {
        if (existing is not CheckBox cb) return false;
        if (!cb.IsFocused) cb.IsChecked = ctx.Resolve(c.Value) is "true";
        cb.Content = ctx.Resolve(c.Label);
        return true;
    }
}

/// <summary>A2UI "Slider" → Slider control.</summary>
public sealed class SliderCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Slider";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        double.TryParse(ctx.Resolve(c.Value), out double val);
        double.TryParse(ctx.Resolve(c.Min), out double min);
        double max = 100;
        if (ctx.Resolve(c.Max) is { } maxStr)
            double.TryParse(maxStr, out max);

        return new Slider { Value = val, Minimum = min, Maximum = max };
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx)
    {
        if (existing is not Slider s) return false;
        if (!s.IsFocused && double.TryParse(ctx.Resolve(c.Value), out double val))
            s.Value = val;
        return true;
    }
}
