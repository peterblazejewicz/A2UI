using A2Ui.Core.Messages;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;

namespace A2Ui.Avalonia.Catalog.Entries;

/// <summary>
/// Evaluates <see cref="CheckRule"/> conditions and wraps a control with
/// inline validation error messages when any check fails.
/// </summary>
internal static class CheckHelper
{
    /// <summary>
    /// Evaluate checks and wrap the control with error messages if any fail.
    /// Returns the original control if no checks exist or all pass.
    /// Always wraps in a StackPanel when checks are defined (even if all pass)
    /// so that <see cref="UpdateChecks"/> can add/remove errors in place.
    /// </summary>
    public static Control ApplyChecks(Control control, A2UiComponent c, IRenderContext ctx)
    {
        if (c.Checks is not { Length: > 0 })
        {
            return control;
        }

        var panel = new StackPanel { Spacing = 2 };
        panel.Children.Add(control);
        AddErrorTextBlocks(panel, c, ctx);
        return panel;
    }

    /// <summary>
    /// Re-evaluate checks and update error TextBlocks in an existing wrapper panel.
    /// Removes old error messages and adds current ones, preserving the first child
    /// (the actual input control) and its focus state.
    /// </summary>
    public static void UpdateChecks(StackPanel wrapper, A2UiComponent c, IRenderContext ctx)
    {
        // Remove old error TextBlocks (everything after the first child)
        while (wrapper.Children.Count > 1)
        {
            wrapper.Children.RemoveAt(wrapper.Children.Count - 1);
        }

        AddErrorTextBlocks(wrapper, c, ctx);
    }

    /// <summary>
    /// Try to find the inner input control inside a CheckHelper wrapper panel.
    /// Returns null if <paramref name="existing"/> is not a check wrapper.
    /// </summary>
    public static T? FindInner<T>(Control existing)
        where T : Control
    {
        if (existing is T direct)
        {
            return direct;
        }

        if (existing is StackPanel panel && panel.Children.Count > 0 && panel.Children[0] is T inner)
        {
            return inner;
        }

        return null;
    }

    /// <summary>
    /// Show only the first failing check message, matching the Lit reference
    /// behavior (which displays <c>validationErrors[0]</c> only).
    /// </summary>
    private static void AddErrorTextBlocks(StackPanel panel, A2UiComponent c, IRenderContext ctx)
    {
        if (c.Checks is not { Length: > 0 })
        {
            return;
        }

        foreach (CheckRule check in c.Checks)
        {
            string? result = EvaluateCondition(check, ctx);
            if (result != "true")
            {
                panel.Children.Add(
                    new TextBlock
                    {
                        Text = check.Message,
                        Classes = { "ValidationError" },
                        FontSize = 12,
                    }
                );
                break; // Only show the first failing message
            }
        }
    }

    /// <summary>
    /// Evaluate all checks on a component. Returns <c>true</c> when every check
    /// passes (condition resolves to "true") or when no checks are defined.
    /// </summary>
    public static bool AllChecksPassing(A2UiComponent c, IRenderContext ctx)
    {
        if (c.Checks is not { Length: > 0 })
        {
            return true;
        }

        foreach (CheckRule check in c.Checks)
        {
            if (EvaluateCondition(check, ctx) != "true")
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Return the message of the first failing check, or <c>null</c> if all pass.
    /// </summary>
    public static string? FirstFailingMessage(A2UiComponent c, IRenderContext ctx)
    {
        if (c.Checks is not { Length: > 0 })
        {
            return null;
        }

        foreach (CheckRule check in c.Checks)
        {
            if (EvaluateCondition(check, ctx) != "true")
            {
                return check.Message;
            }
        }

        return null;
    }

    /// <summary>
    /// Safely evaluate a check condition, catching any resolution exceptions.
    /// Returns <c>null</c> (treated as failed) when evaluation throws.
    /// </summary>
    private static string? EvaluateCondition(CheckRule check, IRenderContext ctx)
    {
        try
        {
            return ctx.Resolve(check.Condition);
        }
        catch (Exception ex)
        {
            if (ctx.Logger is not null)
            {
                CheckLog.ConditionFailed(ctx.Logger, ex);
            }

            return null;
        }
    }
}

internal static partial class CheckLog
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Failed to evaluate check condition")]
    public static partial void ConditionFailed(ILogger logger, Exception exception);
}
