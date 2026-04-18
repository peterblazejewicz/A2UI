using A2Ui.Avalonia.Functions;
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
        Assert.Equal("Hello", this.Eval("capitalize", ("value", "hello")));

    [Fact]
    public void Capitalize_Empty_ReturnsEmpty() => Assert.Equal("", this.Eval("capitalize", ("value", "")));

    [Fact]
    public void Capitalize_Null_ReturnsEmpty() => Assert.Equal("", this.Eval("capitalize", ("value", null)));

    [Fact]
    public void FormatNumber_WithDecimals_ReturnsGrouped()
    {
        Assert.Equal("1,234.50", this.Eval("formatNumber", ("value", "1234.5"), ("decimals", "2")));
    }

    [Fact]
    public void FormatNumber_NoGrouping_ReturnsPlain()
    {
        Assert.Equal(
            "1234.50",
            this.Eval("formatNumber", ("value", "1234.5"), ("decimals", "2"), ("grouping", "false"))
        );
    }

    [Fact]
    public void FormatNumber_InvalidInput_ReturnsEmpty() =>
        Assert.Equal("", this.Eval("formatNumber", ("value", "abc")));

    [Fact]
    public void FormatCurrency_USD_ReturnsFormatted()
    {
        Assert.Equal("$99.99", this.Eval("formatCurrency", ("value", "99.99"), ("currency", "USD")));
    }

    [Fact]
    public void FormatCurrency_LargeValue_ReturnsGrouped()
    {
        Assert.Equal("$1,234.50", this.Eval("formatCurrency", ("value", "1234.5"), ("currency", "USD")));
    }

    [Fact]
    public void FormatCurrency_EUR_ReturnsEuroSymbol()
    {
        Assert.Contains("\u20ac", this.Eval("formatCurrency", ("value", "50"), ("currency", "EUR")));
    }

    [Fact]
    public void FormatDate_CustomFormat_ReturnsFormatted()
    {
        Assert.Equal(
            "March 15, 2026",
            this.Eval("formatDate", ("value", "2026-03-15T10:30:00Z"), ("format", "MMMM d, yyyy"))
        );
    }

    [Fact]
    public void FormatDate_ISO_ReturnsIso8601()
    {
        string? result = this.Eval("formatDate", ("value", "2026-03-15T10:30:00Z"), ("format", "ISO"));
        Assert.NotNull(result);
        Assert.Contains("2026", result);
    }

    [Fact]
    public void FormatDate_InvalidDate_ReturnsEmpty() =>
        Assert.Equal("", this.Eval("formatDate", ("value", "not-a-date"), ("format", "yyyy")));

    [Fact]
    public void FormatDate_DayOfWeek_ReturnsName()
    {
        // 2026-03-15 is a Sunday
        Assert.Equal("Sunday", this.Eval("formatDate", ("value", "2026-03-15T10:30:00Z"), ("format", "EEEE")));
    }

    [Fact]
    public void FormatDate_ShortDayOfWeek_E()
    {
        // 2026-03-15 is a Sunday
        var result = this.Eval("formatDate", ("value", "2026-03-15T00:00:00Z"), ("format", "E"));
        Assert.Equal("Sun", result);
    }

    [Fact]
    public void FormatDate_AmPm_Token()
    {
        var result = this.Eval("formatDate", ("value", "2026-03-15T14:30:00Z"), ("format", "h:mm a"));
        Assert.Equal("2:30 PM", result);
    }

    [Fact]
    public void FormatDate_MixedDayOfWeekAndAmPm()
    {
        var result = this.Eval("formatDate", ("value", "2026-03-15T09:05:00Z"), ("format", "EEEE, h:mm a"));
        Assert.Equal("Sunday, 9:05 AM", result);
    }

    [Fact]
    public void FormatDate_QuotedLiteralContainingA_IsPreserved()
    {
        // TR35 single-quote spans must pass through verbatim. A naïve global
        // a → tt replacement would corrupt the literal 'at' into 'tt' and render
        // "Sunday tt 9:05 AM" instead of "Sunday at 9:05 AM".
        var result = this.Eval("formatDate", ("value", "2026-03-15T09:05:00Z"), ("format", "EEEE 'at' h:mm a"));
        Assert.Equal("Sunday at 9:05 AM", result);
    }

    [Fact]
    public void FormatDate_WideAmPmMarker_MapsToNetTt()
    {
        // TR35 aaaa (wide AM/PM) must collapse to .NET's two-letter tt designator.
        // A per-character replace produced "tttttttt" which DateTime.ToString
        // rejects, and FunctionRegistry.Evaluate swallowed the exception,
        // returning an empty string — a silent regression.
        var result = this.Eval("formatDate", ("value", "2026-03-15T09:05:00Z"), ("format", "h:mm aaaa"));
        Assert.Equal("9:05 AM", result);
    }

    [Fact]
    public void FormatString_IsNotRegistered_ReturnsNull()
    {
        // formatString is a renderer-level special form handled by RenderContext.ResolveFormatString(),
        // not a registry function. Verify it is NOT in the default registry.
        string? result = this.Eval("formatString", ("value", "hello ${name}"));
        Assert.Null(result);
    }

    [Fact]
    public void Pluralize_One_ReturnsSingular() =>
        Assert.Equal("item", this.Eval("pluralize", ("value", "1"), ("one", "item"), ("other", "items")));

    [Fact]
    public void Pluralize_Many_ReturnsPlural() =>
        Assert.Equal("items", this.Eval("pluralize", ("value", "5"), ("one", "item"), ("other", "items")));

    [Fact]
    public void Pluralize_Zero_ReturnsZeroForm() =>
        Assert.Equal(
            "no items",
            this.Eval("pluralize", ("value", "0"), ("zero", "no items"), ("one", "item"), ("other", "items"))
        );

    // ── Arithmetic ──────────────────────────────────────────────────

    [Fact]
    public void Add_TwoNumbers_ReturnsSum() => Assert.Equal("7", this.Eval("add", ("a", "3"), ("b", "4")));

    [Fact]
    public void Subtract_TwoNumbers_ReturnsDifference() =>
        Assert.Equal("7", this.Eval("subtract", ("a", "10"), ("b", "3")));

    [Fact]
    public void Multiply_TwoNumbers_ReturnsProduct() =>
        Assert.Equal("12", this.Eval("multiply", ("a", "3"), ("b", "4")));

    [Fact]
    public void Divide_TwoNumbers_ReturnsQuotient() =>
        Assert.Equal("2.5", this.Eval("divide", ("a", "10"), ("b", "4")));

    [Fact]
    public void Divide_ByZero_ReturnsInfinity() => Assert.Equal("\u221e", this.Eval("divide", ("a", "10"), ("b", "0")));

    // ── Comparison ──────────────────────────────────────────────────

    [Fact]
    public void Equals_SameStrings_ReturnsTrue() =>
        Assert.Equal("true", this.Eval("equals", ("a", "hello"), ("b", "hello")));

    [Fact]
    public void Equals_DifferentStrings_ReturnsFalse() =>
        Assert.Equal("false", this.Eval("equals", ("a", "hello"), ("b", "world")));

    [Fact]
    public void NotEquals_DifferentStrings_ReturnsTrue() =>
        Assert.Equal("true", this.Eval("not_equals", ("a", "hello"), ("b", "world")));

    [Fact]
    public void GreaterThan_LargerFirst_ReturnsTrue() =>
        Assert.Equal("true", this.Eval("greater_than", ("a", "10"), ("b", "5")));

    [Fact]
    public void LessThan_SmallerFirst_ReturnsTrue() =>
        Assert.Equal("true", this.Eval("less_than", ("a", "3"), ("b", "10")));

    [Fact]
    public void LessThan_Equal_ReturnsFalse() => Assert.Equal("false", this.Eval("less_than", ("a", "5"), ("b", "5")));

    // ── Logical ─────────────────────────────────────────────────────

    [Fact]
    public void And_AllTruthy_ReturnsTrue() =>
        Assert.Equal("true", this.Eval("and", ("values", """["yes", "1", "true"]""")));

    [Fact]
    public void And_OneFalsy_ReturnsFalse() =>
        Assert.Equal("false", this.Eval("and", ("values", """["yes", "", "true"]""")));

    [Fact]
    public void Or_OneTruthy_ReturnsTrue() =>
        Assert.Equal("true", this.Eval("or", ("values", """["", "0", "hello"]""")));

    [Fact]
    public void Or_AllFalsy_ReturnsFalse() =>
        Assert.Equal("false", this.Eval("or", ("values", """["", "0", "false"]""")));

    [Fact]
    public void Not_Truthy_ReturnsFalse() => Assert.Equal("false", this.Eval("not", ("value", "hello")));

    [Fact]
    public void Not_Falsy_ReturnsTrue() => Assert.Equal("true", this.Eval("not", ("value", "")));

    // ── String predicates ───────────────────────────────────────────

    [Fact]
    public void Contains_Present_ReturnsTrue() =>
        Assert.Equal("true", this.Eval("contains", ("string", "hello world"), ("substring", "world")));

    [Fact]
    public void Contains_Absent_ReturnsFalse() =>
        Assert.Equal("false", this.Eval("contains", ("string", "hello"), ("substring", "xyz")));

    [Fact]
    public void StartsWith_Match_ReturnsTrue() =>
        Assert.Equal("true", this.Eval("starts_with", ("string", "hello world"), ("prefix", "hello")));

    [Fact]
    public void EndsWith_Match_ReturnsTrue() =>
        Assert.Equal("true", this.Eval("ends_with", ("string", "hello world"), ("suffix", "world")));

    // ── Validation ──────────────────────────────────────────────────

    [Fact]
    public void Required_NonEmpty_ReturnsTrue() => Assert.Equal("true", this.Eval("required", ("value", "something")));

    [Fact]
    public void Required_Empty_ReturnsFalse() => Assert.Equal("false", this.Eval("required", ("value", "")));

    [Fact]
    public void Required_Null_ReturnsFalse() => Assert.Equal("false", this.Eval("required", ("value", null)));

    [Fact]
    public void Email_Valid_ReturnsTrue() => Assert.Equal("true", this.Eval("email", ("value", "test@example.com")));

    [Fact]
    public void Email_Invalid_ReturnsFalse() => Assert.Equal("false", this.Eval("email", ("value", "not-an-email")));

    [Fact]
    public void Regex_Match_ReturnsTrue() =>
        Assert.Equal("true", this.Eval("regex", ("value", "abc123"), ("pattern", @"^[a-z]+\d+$")));

    [Fact]
    public void Regex_NoMatch_ReturnsFalse() =>
        Assert.Equal("false", this.Eval("regex", ("value", "ABC"), ("pattern", @"^\d+$")));

    [Fact]
    public void Length_InRange_ReturnsTrue() =>
        Assert.Equal("true", this.Eval("length", ("value", "hello"), ("min", "3"), ("max", "10")));

    [Fact]
    public void Length_TooShort_ReturnsFalse() =>
        Assert.Equal("false", this.Eval("length", ("value", "hi"), ("min", "3"), ("max", "10")));

    [Fact]
    public void Numeric_InRange_ReturnsTrue() =>
        Assert.Equal("true", this.Eval("numeric", ("value", "5"), ("min", "1"), ("max", "10")));

    [Fact]
    public void Numeric_OutOfRange_ReturnsFalse() =>
        Assert.Equal("false", this.Eval("numeric", ("value", "15"), ("min", "1"), ("max", "10")));

    [Fact]
    public void Numeric_NotANumber_ReturnsFalse() => Assert.Equal("false", this.Eval("numeric", ("value", "abc")));

    // ── Void ────────────────────────────────────────────────────────

    [Fact]
    public void OpenUrl_ReturnsNull() => Assert.Null(this.Eval("openUrl", ("url", "https://example.com")));

    // ── Registry behavior ───────────────────────────────────────────

    [Fact]
    public void UnknownFunction_ReturnsNull() => Assert.Null(this.Eval("nonExistentFunction", ("x", "y")));

    [Fact]
    public void CustomFunction_CanBeRegistered()
    {
        var registry = new FunctionRegistryBuilder()
            .Register("myFunc", args => args.TryGetValue("x", out string? v) ? $"got:{v}" : null)
            .Build();

        Assert.Equal("got:test", registry.Evaluate("myFunc", new Dictionary<string, string?> { ["x"] = "test" }));
    }
}
