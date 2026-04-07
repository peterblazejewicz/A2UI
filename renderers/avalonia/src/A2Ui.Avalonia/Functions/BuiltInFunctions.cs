using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace A2Ui.Avalonia.Functions;

/// <summary>
/// Static methods implementing the A2UI built-in function catalog.
/// Each method takes pre-resolved string arguments and returns a string result.
/// </summary>
internal static class BuiltInFunctions
{
    // ── Cached culture info ────────────────────────────────────────────

    private static readonly NumberFormatInfo s_enUsNumberFormat = CreateEnUsNumberFormat();

    /// <summary>
    /// Create en-US number format, falling back to InvariantCulture in
    /// globalization-invariant mode (Docker containers, trimmed apps).
    /// </summary>
    private static NumberFormatInfo CreateEnUsNumberFormat()
    {
        try
        {
            return CultureInfo.GetCultureInfo("en-US").NumberFormat;
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture.NumberFormat;
        }
    }

    // ── Formatting ──────────────────────────────────────────────────────

    public static string? Capitalize(IReadOnlyDictionary<string, string?> args)
    {
        string? value = GetArg(args, "value");
        if (string.IsNullOrEmpty(value))
            return "";
        return char.ToUpper(value[0], CultureInfo.InvariantCulture) + value[1..];
    }

    public static string? FormatNumber(IReadOnlyDictionary<string, string?> args)
    {
        string? value = GetArg(args, "value");
        if (!TryParseDouble(value, out double num))
            return "";

        int decimals = TryParseInt(GetArg(args, "decimals"), 0);
        // grouping defaults to true
        bool grouping = GetArg(args, "grouping") is not "false";

        if (grouping && decimals == s_enUsNumberFormat.NumberDecimalDigits)
        {
            // Common case: reuse cached format info
            return num.ToString($"N{decimals}", s_enUsNumberFormat);
        }

        var nfi = (NumberFormatInfo)s_enUsNumberFormat.Clone();
        if (!grouping)
            nfi.NumberGroupSeparator = "";

        return num.ToString($"N{decimals}", nfi);
    }

    public static string? FormatCurrency(IReadOnlyDictionary<string, string?> args)
    {
        string? value = GetArg(args, "value");
        if (!TryParseDouble(value, out double num))
            return "";

        string currency = GetArg(args, "currency") ?? "USD";
        int decimals = TryParseInt(GetArg(args, "decimals"), 2);
        bool grouping = GetArg(args, "grouping") is not "false";

        string symbol = currency.ToUpperInvariant() switch
        {
            "USD" => "$",
            "EUR" => "\u20ac",
            "GBP" => "\u00a3",
            "JPY" => "\u00a5",
            _ => currency + " ",
        };

        // Currency always needs clone because we set CurrencySymbol
        var nfi = (NumberFormatInfo)s_enUsNumberFormat.Clone();
        nfi.CurrencySymbol = symbol;
        nfi.CurrencyDecimalDigits = decimals;
        if (!grouping)
            nfi.CurrencyGroupSeparator = "";

        return num.ToString("C", nfi);
    }

    public static string? FormatDate(IReadOnlyDictionary<string, string?> args)
    {
        string? value = GetArg(args, "value");
        if (string.IsNullOrEmpty(value))
            return "";

        if (
            !DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces,
                out DateTime dt
            )
        )
            return "";

        string? format = GetArg(args, "format");
        if (string.IsNullOrEmpty(format))
            return dt.ToString(CultureInfo.InvariantCulture);

        if (format == "ISO")
            return dt.ToString("O", CultureInfo.InvariantCulture);

        // Map Unicode TR35 tokens to .NET format tokens
        string dotNetFormat = MapTr35ToNet(format);
        return dt.ToString(dotNetFormat, CultureInfo.InvariantCulture);
    }

    public static string? FormatString(IReadOnlyDictionary<string, string?> args)
    {
        // For now, return the value template as-is (full expression parsing deferred)
        return GetArg(args, "value") ?? "";
    }

    public static string? Pluralize(IReadOnlyDictionary<string, string?> args)
    {
        string? value = GetArg(args, "value");
        if (!TryParseInt(value, out int n))
            return GetArg(args, "other") ?? "";

        if (n == 0 && args.TryGetValue("zero", out string? zero) && zero is not null)
            return zero;
        if (n == 1 && args.TryGetValue("one", out string? one) && one is not null)
            return one;
        if (n == 2 && args.TryGetValue("two", out string? two) && two is not null)
            return two;

        return GetArg(args, "other") ?? "";
    }

    // ── Arithmetic ──────────────────────────────────────────────────────

    public static string? Add(IReadOnlyDictionary<string, string?> args) => BinaryMath(args, (a, b) => a + b);

    public static string? Subtract(IReadOnlyDictionary<string, string?> args) => BinaryMath(args, (a, b) => a - b);

    public static string? Multiply(IReadOnlyDictionary<string, string?> args) => BinaryMath(args, (a, b) => a * b);

    public static string? Divide(IReadOnlyDictionary<string, string?> args)
    {
        if (!TryParseDouble(GetArg(args, "a"), out double a) || !TryParseDouble(GetArg(args, "b"), out double b))
            return null;
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (b == 0.0)
            return "\u221e"; // ∞
        return (a / b).ToString(CultureInfo.InvariantCulture);
    }

    // ── Comparison ──────────────────────────────────────────────────────

    public static string? Equals(IReadOnlyDictionary<string, string?> args) =>
        BoolResult(GetArg(args, "a") == GetArg(args, "b"));

    public static string? NotEquals(IReadOnlyDictionary<string, string?> args) =>
        BoolResult(GetArg(args, "a") != GetArg(args, "b"));

    public static string? GreaterThan(IReadOnlyDictionary<string, string?> args)
    {
        if (!TryParseDouble(GetArg(args, "a"), out double a) || !TryParseDouble(GetArg(args, "b"), out double b))
            return "false";
        return BoolResult(a > b);
    }

    public static string? LessThan(IReadOnlyDictionary<string, string?> args)
    {
        if (!TryParseDouble(GetArg(args, "a"), out double a) || !TryParseDouble(GetArg(args, "b"), out double b))
            return "false";
        return BoolResult(a > b is false && a != b);
    }

    // ── Logical ─────────────────────────────────────────────────────────

    public static string? And(IReadOnlyDictionary<string, string?> args)
    {
        string? values = GetArg(args, "values");
        if (values is null)
            return "false";
        var items = ParseJsonStringArray(values);
        return BoolResult(items.All(IsTruthy));
    }

    public static string? Or(IReadOnlyDictionary<string, string?> args)
    {
        string? values = GetArg(args, "values");
        if (values is null)
            return "false";
        var items = ParseJsonStringArray(values);
        return BoolResult(items.Any(IsTruthy));
    }

    public static string? Not(IReadOnlyDictionary<string, string?> args) =>
        BoolResult(!IsTruthy(GetArg(args, "value")));

    // ── String predicates ───────────────────────────────────────────────

    public static string? Contains(IReadOnlyDictionary<string, string?> args)
    {
        string? str = GetArg(args, "string");
        string? sub = GetArg(args, "substring");
        if (str is null || sub is null)
            return "false";
        return BoolResult(str.Contains(sub, StringComparison.Ordinal));
    }

    public static string? StartsWith(IReadOnlyDictionary<string, string?> args)
    {
        string? str = GetArg(args, "string");
        string? prefix = GetArg(args, "prefix");
        if (str is null || prefix is null)
            return "false";
        return BoolResult(str.StartsWith(prefix, StringComparison.Ordinal));
    }

    public static string? EndsWith(IReadOnlyDictionary<string, string?> args)
    {
        string? str = GetArg(args, "string");
        string? suffix = GetArg(args, "suffix");
        if (str is null || suffix is null)
            return "false";
        return BoolResult(str.EndsWith(suffix, StringComparison.Ordinal));
    }

    // ── Validation ──────────────────────────────────────────────────────

    public static string? Required(IReadOnlyDictionary<string, string?> args) =>
        BoolResult(!string.IsNullOrEmpty(GetArg(args, "value")));

    public static string? Email(IReadOnlyDictionary<string, string?> args)
    {
        string? value = GetArg(args, "value");
        if (string.IsNullOrEmpty(value))
            return "false";
        return BoolResult(
            Regex.IsMatch(
                value,
                @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
                RegexOptions.NonBacktracking,
                TimeSpan.FromSeconds(1)
            )
        );
    }

    public static string? RegexMatch(IReadOnlyDictionary<string, string?> args)
    {
        string? value = GetArg(args, "value");
        string? pattern = GetArg(args, "pattern");
        if (value is null || pattern is null)
            return "false";

        // Let RegexParseException propagate to FunctionRegistry.Evaluate,
        // which catches ArgumentException (parent of RegexParseException)
        // and logs it. Invalid patterns are user errors worth diagnosing.
        return BoolResult(Regex.IsMatch(value, pattern, RegexOptions.NonBacktracking, TimeSpan.FromSeconds(1)));
    }

    public static string? Length(IReadOnlyDictionary<string, string?> args)
    {
        string? value = GetArg(args, "value");
        int len = value?.Length ?? 0;
        int min = TryParseInt(GetArg(args, "min"), 0);
        int max = TryParseInt(GetArg(args, "max"), int.MaxValue);
        return BoolResult(len >= min && len <= max);
    }

    public static string? Numeric(IReadOnlyDictionary<string, string?> args)
    {
        string? value = GetArg(args, "value");
        if (!TryParseDouble(value, out double num))
            return "false";

        double min = TryParseDouble(GetArg(args, "min"), out double mn) ? mn : double.MinValue;
        double max = TryParseDouble(GetArg(args, "max"), out double mx) ? mx : double.MaxValue;
        return BoolResult(num >= min && num <= max);
    }

    // ── Void ────────────────────────────────────────────────────────────

    public static string? OpenUrl(IReadOnlyDictionary<string, string?> _) => null;

    // ── Helpers ─────────────────────────────────────────────────────────

    private static string? GetArg(IReadOnlyDictionary<string, string?> args, string key) =>
        args.TryGetValue(key, out string? v) ? v : null;

    private static bool TryParseDouble(string? s, out double result) =>
        double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);

    private static bool TryParseInt(string? s, out int result) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    private static int TryParseInt(string? s, int fallback) =>
        int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;

    private static string BoolResult(bool b) => b ? "true" : "false";

    private static string? BinaryMath(IReadOnlyDictionary<string, string?> args, Func<double, double, double> op)
    {
        if (!TryParseDouble(GetArg(args, "a"), out double a) || !TryParseDouble(GetArg(args, "b"), out double b))
            return null;
        return op(a, b).ToString(CultureInfo.InvariantCulture);
    }

    private static bool IsTruthy(string? v) => v is not (null or "" or "false" or "0");

    private static List<string?> ParseJsonStringArray(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var result = new List<string?>();
            foreach (JsonElement el in doc.RootElement.EnumerateArray())
            {
                result.Add(el.ValueKind == JsonValueKind.String ? el.GetString() : el.GetRawText());
            }
            return result;
        }
        catch (JsonException)
        {
            // Return empty list on malformed JSON so And()/Or() evaluate correctly
            // (And on empty → true, Or on empty → false) rather than propagating
            // to FunctionRegistry.Evaluate which would return null.
            return [];
        }
    }

    /// <summary>
    /// Map Unicode TR35 date format tokens to .NET format strings.
    /// Order matters: longer tokens must be replaced first.
    /// </summary>
    private static string MapTr35ToNet(string format)
    {
        // Day-of-week: EEEE → dddd, E → ddd (must be done before dd/d replacements)
        // Use a two-pass approach with placeholders to avoid double-replacement
        string result = format;

        // Replace EEEE/E before touching d/dd (use placeholder to avoid collision)
        result = result.Replace("EEEE", "\x01FULL_DOW\x01");
        result = result.Replace("E", "\x01SHORT_DOW\x01");

        // Now restore placeholders to .NET tokens
        result = result.Replace("\x01FULL_DOW\x01", "dddd");
        result = result.Replace("\x01SHORT_DOW\x01", "ddd");

        // AM/PM: a → tt
        result = result.Replace("a", "tt");

        return result;
    }
}
