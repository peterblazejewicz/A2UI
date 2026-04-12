using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace A2Ui.Avalonia.Catalog;

/// <summary>Standard event name for input value changes.</summary>
internal static class InputEvents
{
    public const string ValueChanged = "valueChanged";
}

/// <summary>A2UI "TextField" → TextBox with two-way data model binding.</summary>
public sealed class TextFieldCatalogEntry : ICatalogEntry
{
    private const string UpdatingTag = "__updating";

    public string ComponentType => "TextField";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var tb = new TextBox
        {
            Text = context.Resolve(component.Value) ?? string.Empty,
            PlaceholderText = context.Resolve(component.Label),
        };

        if (component.Variant is "obscured")
        {
            tb.PasswordChar = '\u2022'; // bullet character
        }

        // Two-way binding: write value back to data model on every text change.
        // Use PropertyChanged (not the TextChanged routed event) so it fires
        // for both user input and programmatic Text assignments.
        // The UpdatingTag guard suppresses events during programmatic updates
        // in Update(), preventing feedback loops and phantom agent events.
        string? bindingPath = component.Value?.Path;
        string componentId = component.Id;

        tb.PropertyChanged += (sender, args) =>
        {
            if (args.Property != TextBox.TextProperty)
            {
                return;
            }

            if (sender is TextBox box && box.Tag is UpdatingTag)
            {
                return;
            }

            InputHelper.NotifyValueChanged(context, bindingPath, tb.Text, componentId);
        };

        return CheckHelper.ApplyChecks(tb, component, context);
    }

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        // Find the TextBox — either directly cached or inside a check wrapper StackPanel
        TextBox? tb = CheckHelper.FindInner<TextBox>(existing);
        if (tb is null)
        {
            return false;
        }

        if (!tb.IsFocused)
        {
            tb.Tag = UpdatingTag;
            tb.Text = context.Resolve(component.Value) ?? string.Empty;
            tb.Tag = null;
        }
        tb.PlaceholderText = context.Resolve(component.Label);

        // Re-evaluate checks in place (preserves focus)
        if (existing is StackPanel wrapper && component.Checks is { Length: > 0 })
        {
            CheckHelper.UpdateChecks(wrapper, component, context);
        }

        return true;
    }
}

/// <summary>
/// A2UI "DateTimeInput" → date picker, time picker, or both.
/// Respects <see cref="A2UiComponent.EnableDate"/> (default true) and
/// <see cref="A2UiComponent.EnableTime"/> (default false) to choose the control layout:
/// <list type="bullet">
///   <item>Date only → <see cref="CalendarDatePicker"/>, output <c>yyyy-MM-dd</c></item>
///   <item>Date + time → <see cref="StackPanel"/> with CalendarDatePicker + TimePicker, output <c>yyyy-MM-ddTHH:mm:ss</c></item>
///   <item>Time only → <see cref="TimePicker"/>, output <c>HH:mm:ss</c></item>
/// </list>
/// </summary>
public sealed class DateTimeInputCatalogEntry : ICatalogEntry
{
    private const string DateTimeTag = "__dateTime";
    private const string TimeOnlyTag = "__timeOnly";

    public string ComponentType => "DateTimeInput";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        bool enableDate = component.EnableDate ?? true;
        bool enableTime = component.EnableTime ?? false;
        string? bindingPath = component.Value?.Path;
        string componentId = component.Id;
        var raw = context.Resolve(component.Value);

        if (enableDate && enableTime)
        {
            return CheckHelper.ApplyChecks(
                CreateDateTimePicker(raw, bindingPath, componentId, context, component),
                component,
                context
            );
        }

        if (!enableDate && enableTime)
        {
            return CheckHelper.ApplyChecks(
                CreateTimePicker(raw, bindingPath, componentId, context),
                component,
                context
            );
        }

        // Default: date only
        return CheckHelper.ApplyChecks(
            CreateDatePicker(raw, bindingPath, componentId, context, component),
            component,
            context
        );
    }

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var raw = context.Resolve(component.Value);

        // Date + time composite panel
        if (CheckHelper.FindInner<StackPanel>(existing) is { Tag: DateTimeTag } panel)
        {
            var datePicker = panel.Children.OfType<CalendarDatePicker>().FirstOrDefault();
            var timePicker = panel.Children.OfType<TimePicker>().FirstOrDefault();
            if (datePicker is null || timePicker is null)
            {
                return false;
            }

            if (!datePicker.IsFocused && raw is not null && DateTime.TryParse(raw, out var dt))
            {
                datePicker.SelectedDate = dt.Date;
                timePicker.SelectedTime = dt.TimeOfDay;
            }

            datePicker.PlaceholderText = context.Resolve(component.Label) ?? "Select date";
            return true;
        }

        // Time only
        if (CheckHelper.FindInner<TimePicker>(existing) is { } tp)
        {
            if (raw is not null && TimeSpan.TryParse(raw, out var ts))
            {
                tp.SelectedTime = ts;
            }

            return true;
        }

        // Date only (original)
        CalendarDatePicker? picker = CheckHelper.FindInner<CalendarDatePicker>(existing);
        if (picker is null)
        {
            return false;
        }

        if (!picker.IsFocused)
        {
            picker.SelectedDate =
                raw is not null && DateOnly.TryParse(raw, out var date) ? date.ToDateTime(TimeOnly.MinValue) : null;
        }

        picker.PlaceholderText = context.Resolve(component.Label) ?? "Select date";

        if (existing is StackPanel wrapper && component.Checks is { Length: > 0 })
        {
            CheckHelper.UpdateChecks(wrapper, component, context);
        }

        return true;
    }

    private static CalendarDatePicker CreateDatePicker(
        string? raw,
        string? bindingPath,
        string componentId,
        IRenderContext ctx,
        A2UiComponent c
    )
    {
        var picker = new CalendarDatePicker { PlaceholderText = ctx.Resolve(c.Label) ?? "Select date" };

        if (raw is not null && DateOnly.TryParse(raw, out var date))
        {
            picker.SelectedDate = date.ToDateTime(TimeOnly.MinValue);
        }

        picker.SelectedDateChanged += (_, _) =>
        {
            string? isoDate = picker.SelectedDate?.ToString("yyyy-MM-dd");
            InputHelper.NotifyValueChanged(ctx, bindingPath, isoDate, componentId);
        };

        return picker;
    }

    private static StackPanel CreateDateTimePicker(
        string? raw,
        string? bindingPath,
        string componentId,
        IRenderContext ctx,
        A2UiComponent c
    )
    {
        var datePicker = new CalendarDatePicker { PlaceholderText = ctx.Resolve(c.Label) ?? "Select date" };
        var timePicker = new TimePicker { ClockIdentifier = "24HourClock" };

        if (raw is not null && DateTime.TryParse(raw, out var dt))
        {
            datePicker.SelectedDate = dt.Date;
            timePicker.SelectedTime = dt.TimeOfDay;
        }

        void NotifyCombined()
        {
            var d = datePicker.SelectedDate;
            var t = timePicker.SelectedTime;
            if (d is null && t is null)
            {
                InputHelper.NotifyValueChanged(ctx, bindingPath, null, componentId);
                return;
            }

            var dateVal = d?.Date ?? DateTime.Today;
            var timeVal = t ?? TimeSpan.Zero;
            string iso = new DateTime(dateVal.Ticks + timeVal.Ticks).ToString("yyyy-MM-ddTHH:mm:ss");
            InputHelper.NotifyValueChanged(ctx, bindingPath, iso, componentId);
        }

        datePicker.SelectedDateChanged += (_, _) => NotifyCombined();
        timePicker.SelectedTimeChanged += (_, _) => NotifyCombined();

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Tag = DateTimeTag,
            Children = { datePicker, timePicker },
        };
    }

    private static TimePicker CreateTimePicker(string? raw, string? bindingPath, string componentId, IRenderContext ctx)
    {
        var timePicker = new TimePicker { ClockIdentifier = "24HourClock", Tag = TimeOnlyTag };

        if (raw is not null && TimeSpan.TryParse(raw, out var ts))
        {
            timePicker.SelectedTime = ts;
        }

        timePicker.SelectedTimeChanged += (_, _) =>
        {
            string? isoTime = timePicker.SelectedTime?.ToString(@"hh\:mm\:ss");
            InputHelper.NotifyValueChanged(ctx, bindingPath, isoTime, componentId);
        };

        return timePicker;
    }
}

/// <summary>
/// A2UI "ChoicePicker" → variant-aware control.
/// - mutuallyExclusive (default): ComboBox, or AutoCompleteBox when filterable.
/// - multipleSelection: ListBox with CheckBox items (checkbox style) or
///   WrapPanel with ToggleButton items (chips style).
/// Value is always a DynamicStringList (array of selected values).
/// </summary>
public sealed class ChoicePickerCatalogEntry : ICatalogEntry
{
    public string ComponentType => "ChoicePicker";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        string? bindingPath = component.Value?.Path;
        string componentId = component.Id;
        bool isMultiple = component.Variant is "multipleSelection";
        var currentValues = ResolveCurrentValues(component.Value, context);

        Control control = isMultiple
            ? CreateMultipleSelection(component, context, bindingPath, componentId, currentValues)
            : CreateMutuallyExclusive(component, context, bindingPath, componentId, currentValues);

        return CheckHelper.ApplyChecks(control, component, context);
    }

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        // Only support in-place update for the simple ComboBox case (mutuallyExclusive, non-filterable).
        // Multi-select and filterable variants require full re-create.
        ComboBox? combo = CheckHelper.FindInner<ComboBox>(existing);
        if (combo is null)
        {
            return false;
        }

        combo.PlaceholderText = context.Resolve(component.Label);

        // Update selected index to match current data model value
        var currentValues = ResolveCurrentValues(component.Value, context);
        int selectedIndex = -1;
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboBoxItem item && item.Tag is string val && currentValues.Contains(val))
            {
                selectedIndex = i;
                break;
            }
        }
        combo.SelectedIndex = selectedIndex;

        if (existing is StackPanel wrapper && component.Checks is { Length: > 0 })
        {
            CheckHelper.UpdateChecks(wrapper, component, context);
        }

        return true;
    }

    private static Control CreateMutuallyExclusive(
        A2UiComponent c,
        IRenderContext ctx,
        string? bindingPath,
        string componentId,
        HashSet<string> currentValues
    )
    {
        if (c.Filterable is true)
        {
            var autoComplete = new AutoCompleteBox
            {
                PlaceholderText = ctx.Resolve(c.Label),
                FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
            };

            if (c.Options is { } opts)
            {
                autoComplete.ItemsSource = opts.Select(o => o.Label).ToArray();
            }

            // Set initial selected text
            if (currentValues.Count > 0 && c.Options is { } options)
            {
                var match = options.FirstOrDefault(o => currentValues.Contains(o.Value));
                if (match is not null)
                {
                    autoComplete.Text = match.Label;
                }
            }

            autoComplete.SelectionChanged += (_, _) =>
            {
                // Map selected label back to value
                string? selectedLabel = autoComplete.SelectedItem as string;
                string? selectedValue = c.Options?.FirstOrDefault(o => o.Label == selectedLabel)?.Value;
                if (selectedValue is not null)
                {
                    InputHelper.NotifyValueChanged(ctx, bindingPath, selectedValue, componentId);
                }
            };

            return autoComplete;
        }

        var combo = new ComboBox { PlaceholderText = ctx.Resolve(c.Label) };

        if (c.Options is { } comboOpts)
        {
            int selectedIndex = -1;
            for (int i = 0; i < comboOpts.Length; i++)
            {
                combo.Items.Add(new ComboBoxItem { Content = comboOpts[i].Label, Tag = comboOpts[i].Value });
                if (currentValues.Contains(comboOpts[i].Value))
                {
                    selectedIndex = i;
                }
            }
            if (selectedIndex >= 0)
            {
                combo.SelectedIndex = selectedIndex;
            }
        }

        combo.SelectionChanged += (_, _) =>
        {
            string? selectedValue = (combo.SelectedItem as ComboBoxItem)?.Tag as string;
            InputHelper.NotifyValueChanged(ctx, bindingPath, selectedValue, componentId);
        };

        return combo;
    }

    private static Control CreateMultipleSelection(
        A2UiComponent c,
        IRenderContext ctx,
        string? bindingPath,
        string componentId,
        HashSet<string> currentValues
    )
    {
        bool useChips = c.DisplayStyle is "chips";

        if (useChips)
        {
            var panel = new WrapPanel { Orientation = Orientation.Horizontal };
            if (c.Options is { } opts)
            {
                foreach (var opt in opts)
                {
                    var toggle = new ToggleButton
                    {
                        Content = opt.Label,
                        Tag = opt.Value,
                        IsChecked = currentValues.Contains(opt.Value),
                        Margin = new Thickness(2),
                    };
                    toggle.Classes.Add("chip");
                    panel.Children.Add(toggle);
                }
            }

            // Fire on any toggle change
            panel.AddHandler(
                ToggleButton.IsCheckedChangedEvent,
                (_, _) =>
                {
                    var selected = panel
                        .Children.OfType<ToggleButton>()
                        .Where(t => t.IsChecked == true)
                        .Select(t => t.Tag as string)
                        .Where(v => v is not null)
                        .ToArray();
                    string serialized = System.Text.Json.JsonSerializer.Serialize(selected);
                    InputHelper.NotifyValueChanged(ctx, bindingPath, serialized, componentId);
                },
                RoutingStrategies.Bubble
            );

            return panel;
        }

        // Default: checkbox-style ListBox
        var listBox = new ListBox { SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle };

        if (c.Options is { } listOpts)
        {
            foreach (var opt in listOpts)
            {
                var item = new ListBoxItem
                {
                    Content = new CheckBox { Content = opt.Label, IsChecked = currentValues.Contains(opt.Value) },
                    Tag = opt.Value,
                };
                listBox.Items.Add(item);
            }
        }

        // SelectionChanged is the source of truth — sync CheckBox visuals from it
        listBox.SelectionChanged += (_, _) =>
        {
            // Sync CheckBox visuals to match actual selection state
            foreach (var lbi in listBox.Items.OfType<ListBoxItem>())
            {
                if (lbi.Content is CheckBox innerCb)
                {
                    innerCb.IsChecked = listBox.SelectedItems!.Contains(lbi);
                }
            }

            var selected = listBox
                .SelectedItems!.OfType<ListBoxItem>()
                .Select(item => item.Tag as string)
                .Where(v => v is not null)
                .ToArray();
            string serialized = System.Text.Json.JsonSerializer.Serialize(selected);
            InputHelper.NotifyValueChanged(ctx, bindingPath, serialized, componentId);
        };

        // Apply initial selection to ListBox model (CheckBox IsChecked was set during construction)
        foreach (var item in listBox.Items.OfType<ListBoxItem>())
        {
            if (item.Content is CheckBox cb && cb.IsChecked == true)
            {
                listBox.SelectedItems!.Add(item);
            }
        }

        return listBox;
    }

    /// <summary>
    /// Resolve the current selected values from the data model.
    /// The value is a DynamicStringList — it may be a JSON array literal or a bound path.
    /// </summary>
    private static HashSet<string> ResolveCurrentValues(DynamicValue? value, IRenderContext ctx)
    {
        if (value is null)
        {
            return [];
        }

        // If it's an array literal, extract string values
        if (value.ArrayLiteral is { } arr)
        {
            var set = new HashSet<string>();
            foreach (var el in arr.EnumerateArray())
            {
                if (el.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    set.Add(el.GetString()!);
                }
            }
            return set;
        }

        // If bound to a path, resolve and try to parse as JSON array
        string? resolved = ctx.Resolve(value);
        if (resolved is null)
        {
            return [];
        }

        // Could be a JSON array string like ["a","b"] or a single value
        if (resolved.StartsWith('['))
        {
            try
            {
                var items = System.Text.Json.JsonSerializer.Deserialize<string[]>(resolved);
                return items is not null ? new HashSet<string>(items) : [];
            }
            catch (System.Text.Json.JsonException)
            {
                return [resolved];
            }
        }

        return [resolved];
    }
}

/// <summary>A2UI "CheckBox" → CheckBox control.</summary>
public sealed class CheckBoxCatalogEntry : ICatalogEntry
{
    private const string UpdatingTag = "__updating";

    public string ComponentType => "CheckBox";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        bool isChecked = context.Resolve(component.Value) is "true";
        var cb = new CheckBox { Content = context.Resolve(component.Label), IsChecked = isChecked };

        cb.IsCheckedChanged += (sender, _) =>
        {
            // Skip events fired by programmatic updates in Update()
            if (sender is CheckBox box && box.Tag is UpdatingTag)
            {
                return;
            }

            context.FireUserAction(InputEvents.ValueChanged, cb.IsChecked == true ? "true" : "false");
        };

        return CheckHelper.ApplyChecks(cb, component, context);
    }

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        CheckBox? cb = CheckHelper.FindInner<CheckBox>(existing);
        if (cb is null)
        {
            return false;
        }

        if (!cb.IsFocused)
        {
            cb.Tag = UpdatingTag;
            cb.IsChecked = context.Resolve(component.Value) is "true";
            cb.Tag = null;
        }
        cb.Content = context.Resolve(component.Label);

        if (existing is StackPanel wrapper && component.Checks is { Length: > 0 })
        {
            CheckHelper.UpdateChecks(wrapper, component, context);
        }

        return true;
    }
}

/// <summary>A2UI "Slider" → Slider control.</summary>
public sealed class SliderCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Slider";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        double.TryParse(context.Resolve(component.Value), out double val);
        double.TryParse(context.Resolve(component.Min), out double min);
        double max = 100;
        if (context.Resolve(component.Max) is { } maxStr)
        {
            double.TryParse(maxStr, out max);
        }

        var slider = new Slider
        {
            Value = val,
            Minimum = min,
            Maximum = max,
        };

        // Fire on thumb drag complete, not on every pixel move
        string? sliderBindingPath = component.Value?.Path;
        string sliderComponentId = component.Id;
        slider.AddHandler(
            Thumb.DragCompletedEvent,
            (_, _) =>
                InputHelper.NotifyValueChanged(
                    context,
                    sliderBindingPath,
                    slider.Value.ToString("G"),
                    sliderComponentId
                ),
            RoutingStrategies.Bubble
        );

        return CheckHelper.ApplyChecks(slider, component, context);
    }

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        Slider? s = CheckHelper.FindInner<Slider>(existing);
        if (s is null)
        {
            return false;
        }

        if (!s.IsFocused && double.TryParse(context.Resolve(component.Value), out double val))
        {
            s.Value = val;
        }

        if (existing is StackPanel wrapper && component.Checks is { Length: > 0 })
        {
            CheckHelper.UpdateChecks(wrapper, component, context);
        }

        return true;
    }
}
