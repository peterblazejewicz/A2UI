using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

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

    private static A2UiComponent MakeComponent(CheckRule[]? checks) =>
        new()
        {
            Id = "c1",
            Component = "TextField",
            Checks = checks,
        };

    // ── ApplyChecks ───────────────────────────────────────────────────────

    [AvaloniaFact]
    public void ApplyChecks_NullChecks_ReturnsOriginalControl()
    {
        var control = new TextBox();
        var component = MakeComponent(null);
        var ctx = MakeCtx();

        Control result = CheckHelper.ApplyChecks(control, component, ctx);

        result.Should().BeSameAs(control, "no checks means no wrapping");
    }

    [AvaloniaFact]
    public void ApplyChecks_AllPassing_WrapsButHasNoErrors()
    {
        var control = new TextBox();
        var component = MakeComponent([PassingCheck("error1"), PassingCheck("error2")]);
        var ctx = MakeCtx();

        Control result = CheckHelper.ApplyChecks(control, component, ctx);

        // Always wraps in StackPanel when checks exist (for in-place UpdateChecks)
        result.Should().BeOfType<StackPanel>();
        var panel = (StackPanel)result;
        panel.Children.Should().HaveCount(1, "all checks pass so no error TextBlocks");
        panel.Children[0].Should().BeSameAs(control);
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
        var panel = result.Should().BeOfType<StackPanel>().Subject;
        panel.Children.Should().HaveCount(2, "original control + first failing error TextBlock only");
        panel.Children[0].Should().BeSameAs(control);

        var tb1 = panel.Children[1].Should().BeOfType<TextBlock>().Subject;
        tb1.Text.Should().Be("ErrorOne", "only the first failing check message is shown");
        tb1.Classes.Should().Contain("ValidationError");
    }

    // ── AllChecksPassing ──────────────────────────────────────────────────

    [AvaloniaFact]
    public void AllChecksPassing_EmptyChecks_ReturnsTrue()
    {
        var component = MakeComponent([]);
        var ctx = MakeCtx();

        bool result = CheckHelper.AllChecksPassing(component, ctx);

        result.Should().BeTrue("an empty checks array means nothing can fail");
    }

    [AvaloniaFact]
    public void AllChecksPassing_OneFailing_ReturnsFalse()
    {
        var component = MakeComponent([PassingCheck(), FailingCheck("Must be valid"), PassingCheck()]);
        var ctx = MakeCtx();

        bool result = CheckHelper.AllChecksPassing(component, ctx);

        result.Should().BeFalse("one failing check must make the result false");
    }

    // ── FirstFailingMessage ───────────────────────────────────────────────

    [AvaloniaFact]
    public void FirstFailingMessage_AllPass_ReturnsNull()
    {
        var component = MakeComponent([PassingCheck("pass1"), PassingCheck("pass2")]);
        var ctx = MakeCtx();

        string? result = CheckHelper.FirstFailingMessage(component, ctx);

        result.Should().BeNull("all checks pass so there is no failing message");
    }

    [AvaloniaFact]
    public void FirstFailingMessage_FirstFails_ReturnsItsMessage()
    {
        var component = MakeComponent([FailingCheck("FirstError"), FailingCheck("SecondError")]);
        var ctx = MakeCtx();

        string? result = CheckHelper.FirstFailingMessage(component, ctx);

        result.Should().Be("FirstError", "should return the message of the first failing check only");
    }
}
