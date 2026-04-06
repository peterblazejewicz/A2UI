using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Media;

namespace A2Ui.Avalonia.Catalog;

/// <summary>
/// Evaluates <see cref="CheckRule"/> conditions and wraps a control with
/// inline validation error messages when any check fails.
/// </summary>
internal static class CheckHelper
{
    /// <summary>
    /// Evaluate checks and wrap the control with error messages if any fail.
    /// Returns the original control if no checks exist or all pass.
    /// </summary>
    public static Control ApplyChecks(Control control, A2UiComponent c, IRenderContext ctx)
    {
        if (c.Checks is not { Length: > 0 })
            return control;

        var errors = new List<string>();
        foreach (CheckRule check in c.Checks)
        {
            string? result = ctx.Resolve(check.Condition);
            if (result != "true")
                errors.Add(check.Message);
        }

        if (errors.Count == 0)
            return control;

        var panel = new StackPanel { Spacing = 2 };
        panel.Children.Add(control);
        foreach (string error in errors)
        {
            panel.Children.Add(new TextBlock
            {
                Text = error,
                Classes = { "ValidationError" },
                Foreground = new SolidColorBrush(Color.Parse("#E57373")),
                FontSize = 12,
            });
        }

        return panel;
    }

    /// <summary>
    /// Evaluate all checks on a component. Returns <c>true</c> when every check
    /// passes (condition resolves to "true") or when no checks are defined.
    /// </summary>
    public static bool AllChecksPassing(A2UiComponent c, IRenderContext ctx)
    {
        if (c.Checks is not { Length: > 0 })
            return true;

        foreach (CheckRule check in c.Checks)
        {
            if (ctx.Resolve(check.Condition) != "true")
                return false;
        }

        return true;
    }

    /// <summary>
    /// Return the message of the first failing check, or <c>null</c> if all pass.
    /// </summary>
    public static string? FirstFailingMessage(A2UiComponent c, IRenderContext ctx)
    {
        if (c.Checks is not { Length: > 0 })
            return null;

        foreach (CheckRule check in c.Checks)
        {
            if (ctx.Resolve(check.Condition) != "true")
                return check.Message;
        }

        return null;
    }
}
