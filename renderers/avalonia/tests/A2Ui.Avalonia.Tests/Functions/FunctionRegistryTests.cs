using A2Ui.Avalonia.Functions;
using FluentAssertions;
using Xunit;

namespace A2Ui.Avalonia.Tests.Functions;

public sealed class FunctionRegistryTests
{
    private readonly FunctionRegistry _registry = FunctionRegistry.CreateDefault();

    private string? Eval(string fn, params (string key, string? value)[] args)
    {
        var dict = new Dictionary<string, string?>();
        foreach (var (key, value) in args)
            dict[key] = value;
        return _registry.Evaluate(fn, dict);
    }

    // ── Formatting ──────────────────────────────────────────────────

    [Fact]
    public void Capitalize_NormalString_ReturnsCapitalized() =>
        Eval("capitalize", ("value", "hello")).Should().Be("Hello");

    [Fact]
    public void Capitalize_Empty_ReturnsEmpty() =>
        Eval("capitalize", ("value", "")).Should().Be("");

    [Fact]
    public void Capitalize_Null_ReturnsEmpty() =>
        Eval("capitalize", ("value", null)).Should().Be("");

    [Fact]
    public void FormatNumber_WithDecimals_ReturnsGrouped()
    {
        Eval("formatNumber", ("value", "1234.5"), ("decimals", "2"))
            .Should().Be("1,234.50");
    }

    [Fact]
    public void FormatNumber_NoGrouping_ReturnsPlain()
    {
        Eval("formatNumber", ("value", "1234.5"), ("decimals", "2"), ("grouping", "false"))
            .Should().Be("1234.50");
    }

    [Fact]
    public void FormatNumber_InvalidInput_ReturnsEmpty() =>
        Eval("formatNumber", ("value", "abc")).Should().Be("");

    [Fact]
    public void FormatCurrency_USD_ReturnsFormatted()
    {
        Eval("formatCurrency", ("value", "99.99"), ("currency", "USD"))
            .Should().Be("$99.99");
    }

    [Fact]
    public void FormatCurrency_LargeValue_ReturnsGrouped()
    {
        Eval("formatCurrency", ("value", "1234.5"), ("currency", "USD"))
            .Should().Be("$1,234.50");
    }

    [Fact]
    public void FormatCurrency_EUR_ReturnsEuroSymbol()
    {
        Eval("formatCurrency", ("value", "50"), ("currency", "EUR"))
            .Should().Contain("\u20ac");
    }

    [Fact]
    public void FormatDate_CustomFormat_ReturnsFormatted()
    {
        Eval("formatDate", ("value", "2026-03-15T10:30:00Z"), ("format", "MMMM d, yyyy"))
            .Should().Be("March 15, 2026");
    }

    [Fact]
    public void FormatDate_ISO_ReturnsIso8601()
    {
        string? result = Eval("formatDate", ("value", "2026-03-15T10:30:00Z"), ("format", "ISO"));
        result.Should().NotBeNull();
        result.Should().Contain("2026");
    }

    [Fact]
    public void FormatDate_InvalidDate_ReturnsEmpty() =>
        Eval("formatDate", ("value", "not-a-date"), ("format", "yyyy")).Should().Be("");

    [Fact]
    public void FormatDate_DayOfWeek_ReturnsName()
    {
        // 2026-03-15 is a Sunday
        Eval("formatDate", ("value", "2026-03-15T10:30:00Z"), ("format", "EEEE"))
            .Should().Be("Sunday");
    }

    [Fact]
    public void FormatDate_ShortDayOfWeek_E()
    {
        // 2026-03-15 is a Sunday
        var result = Eval("formatDate", ("value", "2026-03-15T00:00:00Z"), ("format", "E"));
        result.Should().Be("Sun");
    }

    [Fact]
    public void FormatDate_AmPm_Token()
    {
        var result = Eval("formatDate", ("value", "2026-03-15T14:30:00Z"), ("format", "h:mm a"));
        result.Should().Be("2:30 PM");
    }

    [Fact]
    public void FormatDate_MixedDayOfWeekAndAmPm()
    {
        var result = Eval("formatDate", ("value", "2026-03-15T09:05:00Z"), ("format", "EEEE, h:mm a"));
        result.Should().Be("Sunday, 9:05 AM");
    }

    [Fact]
    public void FormatString_ReturnsValueAsIs() =>
        Eval("formatString", ("value", "Hello ${/name}")).Should().Be("Hello ${/name}");

    [Fact]
    public void Pluralize_One_ReturnsSingular() =>
        Eval("pluralize", ("value", "1"), ("one", "item"), ("other", "items"))
            .Should().Be("item");

    [Fact]
    public void Pluralize_Many_ReturnsPlural() =>
        Eval("pluralize", ("value", "5"), ("one", "item"), ("other", "items"))
            .Should().Be("items");

    [Fact]
    public void Pluralize_Zero_ReturnsZeroForm() =>
        Eval("pluralize", ("value", "0"), ("zero", "no items"), ("one", "item"), ("other", "items"))
            .Should().Be("no items");

    // ── Arithmetic ──────────────────────────────────────────────────

    [Fact]
    public void Add_TwoNumbers_ReturnsSum() =>
        Eval("add", ("a", "3"), ("b", "4")).Should().Be("7");

    [Fact]
    public void Subtract_TwoNumbers_ReturnsDifference() =>
        Eval("subtract", ("a", "10"), ("b", "3")).Should().Be("7");

    [Fact]
    public void Multiply_TwoNumbers_ReturnsProduct() =>
        Eval("multiply", ("a", "3"), ("b", "4")).Should().Be("12");

    [Fact]
    public void Divide_TwoNumbers_ReturnsQuotient() =>
        Eval("divide", ("a", "10"), ("b", "4")).Should().Be("2.5");

    [Fact]
    public void Divide_ByZero_ReturnsInfinity() =>
        Eval("divide", ("a", "10"), ("b", "0")).Should().Be("\u221e");

    // ── Comparison ──────────────────────────────────────────────────

    [Fact]
    public void Equals_SameStrings_ReturnsTrue() =>
        Eval("equals", ("a", "hello"), ("b", "hello")).Should().Be("true");

    [Fact]
    public void Equals_DifferentStrings_ReturnsFalse() =>
        Eval("equals", ("a", "hello"), ("b", "world")).Should().Be("false");

    [Fact]
    public void NotEquals_DifferentStrings_ReturnsTrue() =>
        Eval("not_equals", ("a", "hello"), ("b", "world")).Should().Be("true");

    [Fact]
    public void GreaterThan_LargerFirst_ReturnsTrue() =>
        Eval("greater_than", ("a", "10"), ("b", "5")).Should().Be("true");

    [Fact]
    public void LessThan_SmallerFirst_ReturnsTrue() =>
        Eval("less_than", ("a", "3"), ("b", "10")).Should().Be("true");

    [Fact]
    public void LessThan_Equal_ReturnsFalse() =>
        Eval("less_than", ("a", "5"), ("b", "5")).Should().Be("false");

    // ── Logical ─────────────────────────────────────────────────────

    [Fact]
    public void And_AllTruthy_ReturnsTrue() =>
        Eval("and", ("values", """["yes", "1", "true"]""")).Should().Be("true");

    [Fact]
    public void And_OneFalsy_ReturnsFalse() =>
        Eval("and", ("values", """["yes", "", "true"]""")).Should().Be("false");

    [Fact]
    public void Or_OneTruthy_ReturnsTrue() =>
        Eval("or", ("values", """["", "0", "hello"]""")).Should().Be("true");

    [Fact]
    public void Or_AllFalsy_ReturnsFalse() =>
        Eval("or", ("values", """["", "0", "false"]""")).Should().Be("false");

    [Fact]
    public void Not_Truthy_ReturnsFalse() =>
        Eval("not", ("value", "hello")).Should().Be("false");

    [Fact]
    public void Not_Falsy_ReturnsTrue() =>
        Eval("not", ("value", "")).Should().Be("true");

    // ── String predicates ───────────────────────────────────────────

    [Fact]
    public void Contains_Present_ReturnsTrue() =>
        Eval("contains", ("string", "hello world"), ("substring", "world")).Should().Be("true");

    [Fact]
    public void Contains_Absent_ReturnsFalse() =>
        Eval("contains", ("string", "hello"), ("substring", "xyz")).Should().Be("false");

    [Fact]
    public void StartsWith_Match_ReturnsTrue() =>
        Eval("starts_with", ("string", "hello world"), ("prefix", "hello")).Should().Be("true");

    [Fact]
    public void EndsWith_Match_ReturnsTrue() =>
        Eval("ends_with", ("string", "hello world"), ("suffix", "world")).Should().Be("true");

    // ── Validation ──────────────────────────────────────────────────

    [Fact]
    public void Required_NonEmpty_ReturnsTrue() =>
        Eval("required", ("value", "something")).Should().Be("true");

    [Fact]
    public void Required_Empty_ReturnsFalse() =>
        Eval("required", ("value", "")).Should().Be("false");

    [Fact]
    public void Required_Null_ReturnsFalse() =>
        Eval("required", ("value", null)).Should().Be("false");

    [Fact]
    public void Email_Valid_ReturnsTrue() =>
        Eval("email", ("value", "test@example.com")).Should().Be("true");

    [Fact]
    public void Email_Invalid_ReturnsFalse() =>
        Eval("email", ("value", "not-an-email")).Should().Be("false");

    [Fact]
    public void Regex_Match_ReturnsTrue() =>
        Eval("regex", ("value", "abc123"), ("pattern", @"^[a-z]+\d+$")).Should().Be("true");

    [Fact]
    public void Regex_NoMatch_ReturnsFalse() =>
        Eval("regex", ("value", "ABC"), ("pattern", @"^\d+$")).Should().Be("false");

    [Fact]
    public void Length_InRange_ReturnsTrue() =>
        Eval("length", ("value", "hello"), ("min", "3"), ("max", "10")).Should().Be("true");

    [Fact]
    public void Length_TooShort_ReturnsFalse() =>
        Eval("length", ("value", "hi"), ("min", "3"), ("max", "10")).Should().Be("false");

    [Fact]
    public void Numeric_InRange_ReturnsTrue() =>
        Eval("numeric", ("value", "5"), ("min", "1"), ("max", "10")).Should().Be("true");

    [Fact]
    public void Numeric_OutOfRange_ReturnsFalse() =>
        Eval("numeric", ("value", "15"), ("min", "1"), ("max", "10")).Should().Be("false");

    [Fact]
    public void Numeric_NotANumber_ReturnsFalse() =>
        Eval("numeric", ("value", "abc")).Should().Be("false");

    // ── Void ────────────────────────────────────────────────────────

    [Fact]
    public void OpenUrl_ReturnsNull() =>
        Eval("openUrl", ("url", "https://example.com")).Should().BeNull();

    // ── Registry behavior ───────────────────────────────────────────

    [Fact]
    public void UnknownFunction_ReturnsNull() =>
        Eval("nonExistentFunction", ("x", "y")).Should().BeNull();

    [Fact]
    public void CustomFunction_CanBeRegistered()
    {
        var registry = new FunctionRegistryBuilder()
            .Register("myFunc", args =>
                args.TryGetValue("x", out string? v) ? $"got:{v}" : null)
            .Build();

        registry.Evaluate("myFunc", new Dictionary<string, string?> { ["x"] = "test" })
            .Should().Be("got:test");
    }
}
