using A2Ui.Avalonia.Catalog;
using A2Ui.Avalonia.Catalog.Entries;
using A2Ui.Core;
using A2Ui.Core.Bindings;
using A2Ui.Core.Components;
using A2Ui.Core.Messages;
using A2Ui.Core.Surfaces;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace A2Ui.Avalonia.Tests.Catalog;

/// <summary>
/// Unit tests for <see cref="CheckHelper"/>.
/// </summary>
public sealed class CheckHelperTests
{
    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// A render context that resolves <see cref="DynamicValue"/> string literals directly,
    /// letting tests control pass/fail by using "true" or any other string.
    /// </summary>
    private static MockRenderContext MakeCtx() => new(new DataModel());

    private static CheckRule PassingCheck(string message = "ok") =>
        new() { Condition = DynamicValue.FromString("true"), Message = message };

    private static CheckRule FailingCheck(string message = "Fail message") =>
        new() { Condition = DynamicValue.FromString("false"), Message = message };

    private static TextFieldComponent MakeComponent(CheckRule[]? checks) => new() { Id = "c1", Checks = checks };

    // ── ApplyChecks ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void ApplyChecks_NullChecks_ReturnsOriginalControl()
    {
        var control = new TextBox();
        var component = MakeComponent(null);
        var ctx = MakeCtx();

        Control result = CheckHelper.ApplyChecks(control, component, ctx);

        Assert.Same(control, result);
    }

    [AvaloniaFact]
    public void ApplyChecks_AllPassing_WrapsButHasNoErrors()
    {
        var control = new TextBox();
        var component = MakeComponent([PassingCheck("error1"), PassingCheck("error2")]);
        var ctx = MakeCtx();

        Control result = CheckHelper.ApplyChecks(control, component, ctx);

        // Always wraps in StackPanel when checks exist (for in-place UpdateChecks)
        Assert.IsType<StackPanel>(result);
        var panel = (StackPanel)result;
        Assert.Single(panel.Children);
        Assert.Same(control, panel.Children[0]);
    }

    [AvaloniaFact]
    public void ApplyChecks_MixedPassFail_AddsOnlyFailingMessages()
    {
        var control = new TextBox();
        var component = MakeComponent([
            PassingCheck("ShouldNotAppear"),
            FailingCheck("ErrorOne"),
            PassingCheck("AlsoShouldNotAppear"),
            FailingCheck("ErrorTwo"),
        ]);
        var ctx = MakeCtx();

        Control result = CheckHelper.ApplyChecks(control, component, ctx);

        // Result must be a StackPanel wrapping the original control + first failing error only
        var panel = Assert.IsType<StackPanel>(result);
        Assert.Equal(2, panel.Children.Count);
        Assert.Same(control, panel.Children[0]);

        var tb1 = Assert.IsType<TextBlock>(panel.Children[1]);
        Assert.Equal("ErrorOne", tb1.Text);
        Assert.Contains("ValidationError", tb1.Classes);
    }

    // ── AllChecksPassing ──────────────────────────────────────────────────

    [AvaloniaFact]
    public void AllChecksPassing_EmptyChecks_ReturnsTrue()
    {
        var component = MakeComponent([]);
        var ctx = MakeCtx();

        bool result = CheckHelper.AllChecksPassing(component, ctx);

        Assert.True(result);
    }

    [AvaloniaFact]
    public void AllChecksPassing_OneFailing_ReturnsFalse()
    {
        var component = MakeComponent([PassingCheck(), FailingCheck("Must be valid"), PassingCheck()]);
        var ctx = MakeCtx();

        bool result = CheckHelper.AllChecksPassing(component, ctx);

        Assert.False(result);
    }

    // ── FirstFailingMessage ───────────────────────────────────────────────

    [AvaloniaFact]
    public void FirstFailingMessage_AllPass_ReturnsNull()
    {
        var component = MakeComponent([PassingCheck("pass1"), PassingCheck("pass2")]);
        var ctx = MakeCtx();

        string? result = CheckHelper.FirstFailingMessage(component, ctx);

        Assert.Null(result);
    }

    [AvaloniaFact]
    public void FirstFailingMessage_FirstFails_ReturnsItsMessage()
    {
        var component = MakeComponent([FailingCheck("FirstError"), FailingCheck("SecondError")]);
        var ctx = MakeCtx();

        string? result = CheckHelper.FirstFailingMessage(component, ctx);

        Assert.Equal("FirstError", result);
    }
}
