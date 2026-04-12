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
        {
            dict[key] = value;
        }

        return this._registry.Evaluate(fn, dict);
    }

    // ── Formatting ──────────────────────────────────────────────────

    [Fact]
    public void Capitalize_NormalString_ReturnsCapitalized() =>
        this.Eval("capitalize", ("value", "hello")).Should().Be("Hello");

    [Fact]
    public void Capitalize_Empty_ReturnsEmpty() => this.Eval("capitalize", ("value", "")).Should().Be("");

    [Fact]
    public void Capitalize_Null_ReturnsEmpty() => this.Eval("capitalize", ("value", null)).Should().Be("");

    [Fact]
    public void FormatNumber_WithDecimals_ReturnsGrouped()
    {
        this.Eval("formatNumber", ("value", "1234.5"), ("decimals", "2")).Should().Be("1,234.50");
    }

    [Fact]
    public void FormatNumber_NoGrouping_ReturnsPlain()
    {
        this.Eval("formatNumber", ("value", "1234.5"), ("decimals", "2"), ("grouping", "false")).Should().Be("1234.50");
    }

    [Fact]
    public void FormatNumber_InvalidInput_ReturnsEmpty() => this.Eval("formatNumber", ("value", "abc")).Should().Be("");

    [Fact]
    public void FormatCurrency_USD_ReturnsFormatted()
    {
        this.Eval("formatCurrency", ("value", "99.99"), ("currency", "USD")).Should().Be("$99.99");
    }

    [Fact]
    public void FormatCurrency_LargeValue_ReturnsGrouped()
    {
        this.Eval("formatCurrency", ("value", "1234.5"), ("currency", "USD")).Should().Be("$1,234.50");
    }

    [Fact]
    public void FormatCurrency_EUR_ReturnsEuroSymbol()
    {
        this.Eval("formatCurrency", ("value", "50"), ("currency", "EUR")).Should().Contain("\u20ac");
    }

    [Fact]
    public void FormatDate_CustomFormat_ReturnsFormatted()
    {
        this.Eval("formatDate", ("value", "2026-03-15T10:30:00Z"), ("format", "MMMM d, yyyy"))
            .Should()
            .Be("March 15, 2026");
    }

    [Fact]
    public void FormatDate_ISO_ReturnsIso8601()
    {
        string? result = this.Eval("formatDate", ("value", "2026-03-15T10:30:00Z"), ("format", "ISO"));
        result.Should().NotBeNull();
        result.Should().Contain("2026");
    }

    [Fact]
    public void FormatDate_InvalidDate_ReturnsEmpty() =>
        this.Eval("formatDate", ("value", "not-a-date"), ("format", "yyyy")).Should().Be("");

    [Fact]
    public void FormatDate_DayOfWeek_ReturnsName()
    {
        // 2026-03-15 is a Sunday
        this.Eval("formatDate", ("value", "2026-03-15T10:30:00Z"), ("format", "EEEE")).Should().Be("Sunday");
    }

    [Fact]
    public void FormatDate_ShortDayOfWeek_E()
    {
        // 2026-03-15 is a Sunday
        var result = this.Eval("formatDate", ("value", "2026-03-15T00:00:00Z"), ("format", "E"));
        result.Should().Be("Sun");
    }

    [Fact]
    public void FormatDate_AmPm_Token()
    {
        var result = this.Eval("formatDate", ("value", "2026-03-15T14:30:00Z"), ("format", "h:mm a"));
        result.Should().Be("2:30 PM");
    }

    [Fact]
    public void FormatDate_MixedDayOfWeekAndAmPm()
    {
        var result = this.Eval("formatDate", ("value", "2026-03-15T09:05:00Z"), ("format", "EEEE, h:mm a"));
        result.Should().Be("Sunday, 9:05 AM");
    }

    [Fact]
    public void FormatString_ReturnsValueAsIs() =>
        this.Eval("formatString", ("value", "Hello ${/name}")).Should().Be("Hello ${/name}");

    [Fact]
    public void FormatString_MalformedTemplate_ReturnsFallback()
    {
        // The FunctionRegistry layer passes the value arg through as-is (no expression parsing).
        // A malformed template like "${" should be returned unchanged rather than crashing.
        // Full expression parsing (and the fallback-on-parse-error path) lives in A2UiRenderer;
        // at the registry level the contract is simply: return the value string.
        string? result = this.Eval("formatString", ("value", "${"));
        result.Should().Be("${", "formatString at registry level must not crash on malformed input");
    }

    [Fact]
    public void Pluralize_One_ReturnsSingular() =>
        this.Eval("pluralize", ("value", "1"), ("one", "item"), ("other", "items")).Should().Be("item");

    [Fact]
    public void Pluralize_Many_ReturnsPlural() =>
        this.Eval("pluralize", ("value", "5"), ("one", "item"), ("other", "items")).Should().Be("items");

    [Fact]
    public void Pluralize_Zero_ReturnsZeroForm() =>
        this.Eval("pluralize", ("value", "0"), ("zero", "no items"), ("one", "item"), ("other", "items"))
            .Should()
            .Be("no items");

    // ── Arithmetic ──────────────────────────────────────────────────

    [Fact]
    public void Add_TwoNumbers_ReturnsSum() => this.Eval("add", ("a", "3"), ("b", "4")).Should().Be("7");

    [Fact]
    public void Subtract_TwoNumbers_ReturnsDifference() =>
        this.Eval("subtract", ("a", "10"), ("b", "3")).Should().Be("7");

    [Fact]
    public void Multiply_TwoNumbers_ReturnsProduct() => this.Eval("multiply", ("a", "3"), ("b", "4")).Should().Be("12");

    [Fact]
    public void Divide_TwoNumbers_ReturnsQuotient() => this.Eval("divide", ("a", "10"), ("b", "4")).Should().Be("2.5");

    [Fact]
    public void Divide_ByZero_ReturnsInfinity() => this.Eval("divide", ("a", "10"), ("b", "0")).Should().Be("\u221e");

    // ── Comparison ──────────────────────────────────────────────────

    [Fact]
    public void Equals_SameStrings_ReturnsTrue() =>
        this.Eval("equals", ("a", "hello"), ("b", "hello")).Should().Be("true");

    [Fact]
    public void Equals_DifferentStrings_ReturnsFalse() =>
        this.Eval("equals", ("a", "hello"), ("b", "world")).Should().Be("false");

    [Fact]
    public void NotEquals_DifferentStrings_ReturnsTrue() =>
        this.Eval("not_equals", ("a", "hello"), ("b", "world")).Should().Be("true");

    [Fact]
    public void GreaterThan_LargerFirst_ReturnsTrue() =>
        this.Eval("greater_than", ("a", "10"), ("b", "5")).Should().Be("true");

    [Fact]
    public void LessThan_SmallerFirst_ReturnsTrue() =>
        this.Eval("less_than", ("a", "3"), ("b", "10")).Should().Be("true");

    [Fact]
    public void LessThan_Equal_ReturnsFalse() => this.Eval("less_than", ("a", "5"), ("b", "5")).Should().Be("false");

    // ── Logical ─────────────────────────────────────────────────────

    [Fact]
    public void And_AllTruthy_ReturnsTrue() =>
        this.Eval("and", ("values", """["yes", "1", "true"]""")).Should().Be("true");

    [Fact]
    public void And_OneFalsy_ReturnsFalse() =>
        this.Eval("and", ("values", """["yes", "", "true"]""")).Should().Be("false");

    [Fact]
    public void Or_OneTruthy_ReturnsTrue() => this.Eval("or", ("values", """["", "0", "hello"]""")).Should().Be("true");

    [Fact]
    public void Or_AllFalsy_ReturnsFalse() =>
        this.Eval("or", ("values", """["", "0", "false"]""")).Should().Be("false");

    [Fact]
    public void Not_Truthy_ReturnsFalse() => this.Eval("not", ("value", "hello")).Should().Be("false");

    [Fact]
    public void Not_Falsy_ReturnsTrue() => this.Eval("not", ("value", "")).Should().Be("true");

    // ── String predicates ───────────────────────────────────────────

    [Fact]
    public void Contains_Present_ReturnsTrue() =>
        this.Eval("contains", ("string", "hello world"), ("substring", "world")).Should().Be("true");

    [Fact]
    public void Contains_Absent_ReturnsFalse() =>
        this.Eval("contains", ("string", "hello"), ("substring", "xyz")).Should().Be("false");

    [Fact]
    public void StartsWith_Match_ReturnsTrue() =>
        this.Eval("starts_with", ("string", "hello world"), ("prefix", "hello")).Should().Be("true");

    [Fact]
    public void EndsWith_Match_ReturnsTrue() =>
        this.Eval("ends_with", ("string", "hello world"), ("suffix", "world")).Should().Be("true");

    // ── Validation ──────────────────────────────────────────────────

    [Fact]
    public void Required_NonEmpty_ReturnsTrue() => this.Eval("required", ("value", "something")).Should().Be("true");

    [Fact]
    public void Required_Empty_ReturnsFalse() => this.Eval("required", ("value", "")).Should().Be("false");

    [Fact]
    public void Required_Null_ReturnsFalse() => this.Eval("required", ("value", null)).Should().Be("false");

    [Fact]
    public void Email_Valid_ReturnsTrue() => this.Eval("email", ("value", "test@example.com")).Should().Be("true");

    [Fact]
    public void Email_Invalid_ReturnsFalse() => this.Eval("email", ("value", "not-an-email")).Should().Be("false");

    [Fact]
    public void Regex_Match_ReturnsTrue() =>
        this.Eval("regex", ("value", "abc123"), ("pattern", @"^[a-z]+\d+$")).Should().Be("true");

    [Fact]
    public void Regex_NoMatch_ReturnsFalse() =>
        this.Eval("regex", ("value", "ABC"), ("pattern", @"^\d+$")).Should().Be("false");

    [Fact]
    public void Length_InRange_ReturnsTrue() =>
        this.Eval("length", ("value", "hello"), ("min", "3"), ("max", "10")).Should().Be("true");

    [Fact]
    public void Length_TooShort_ReturnsFalse() =>
        this.Eval("length", ("value", "hi"), ("min", "3"), ("max", "10")).Should().Be("false");

    [Fact]
    public void Numeric_InRange_ReturnsTrue() =>
        this.Eval("numeric", ("value", "5"), ("min", "1"), ("max", "10")).Should().Be("true");

    [Fact]
    public void Numeric_OutOfRange_ReturnsFalse() =>
        this.Eval("numeric", ("value", "15"), ("min", "1"), ("max", "10")).Should().Be("false");

    [Fact]
    public void Numeric_NotANumber_ReturnsFalse() => this.Eval("numeric", ("value", "abc")).Should().Be("false");

    // ── Void ────────────────────────────────────────────────────────

    [Fact]
    public void OpenUrl_ReturnsNull() => this.Eval("openUrl", ("url", "https://example.com")).Should().BeNull();

    // ── Registry behavior ───────────────────────────────────────────

    [Fact]
    public void UnknownFunction_ReturnsNull() => this.Eval("nonExistentFunction", ("x", "y")).Should().BeNull();

    [Fact]
    public void CustomFunction_CanBeRegistered()
    {
        var registry = new FunctionRegistryBuilder()
            .Register("myFunc", args => args.TryGetValue("x", out string? v) ? $"got:{v}" : null)
            .Build();

        registry.Evaluate("myFunc", new Dictionary<string, string?> { ["x"] = "test" }).Should().Be("got:test");
    }
}
