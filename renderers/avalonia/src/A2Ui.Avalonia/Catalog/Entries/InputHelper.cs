namespace A2Ui.Avalonia.Catalog.Entries;

/// <summary>
/// Shared helper for input catalog entries that need two-way data model binding.
/// Centralizes the write-back + fire-action pattern used by TextField, DateTimeInput,
/// Slider, and ChoicePicker.
/// </summary>
internal static class InputHelper
{
    /// <summary>
    /// Write a value back to the data model (if bound) and fire the user action event.
    /// </summary>
    internal static void NotifyValueChanged(
        IRenderContext ctx,
        string? bindingPath,
        string? value,
        string? componentId = null
    )
    {
        if (bindingPath is not null)
        {
            ctx.UpdateDataModel(bindingPath, value);
        }

        ctx.FireUserAction(InputEvents.ValueChanged, value, componentId);
    }
}
