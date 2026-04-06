namespace A2Ui.Avalonia.Functions;

/// <summary>
/// Dictionary-based registry mapping function names to evaluation delegates.
/// Thread-safe for reads after construction (register all functions before use).
/// </summary>
public sealed class FunctionRegistry : IFunctionRegistry
{
    private readonly Dictionary<string, Func<IReadOnlyDictionary<string, string?>, string?>> _functions = new();

    /// <summary>Register a named function. Returns this for chaining.</summary>
    public FunctionRegistry Register(string name,
        Func<IReadOnlyDictionary<string, string?>, string?> fn)
    {
        _functions[name] = fn;
        return this;
    }

    /// <inheritdoc />
    public string? Evaluate(string functionName, IReadOnlyDictionary<string, string?> resolvedArgs)
    {
        if (!_functions.TryGetValue(functionName, out var fn))
        {
            System.Diagnostics.Trace.TraceWarning(
                $"[FunctionRegistry] Unknown function: {functionName}");
            return null;
        }

        try
        {
            return fn(resolvedArgs);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                $"[FunctionRegistry] Error evaluating '{functionName}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Create a registry pre-loaded with all A2UI built-in functions.
    /// </summary>
    public static FunctionRegistry CreateDefault() => new FunctionRegistry()
        // Formatting
        .Register("capitalize", BuiltInFunctions.Capitalize)
        .Register("formatNumber", BuiltInFunctions.FormatNumber)
        .Register("formatCurrency", BuiltInFunctions.FormatCurrency)
        .Register("formatDate", BuiltInFunctions.FormatDate)
        .Register("formatString", BuiltInFunctions.FormatString)
        .Register("pluralize", BuiltInFunctions.Pluralize)
        // Arithmetic
        .Register("add", BuiltInFunctions.Add)
        .Register("subtract", BuiltInFunctions.Subtract)
        .Register("multiply", BuiltInFunctions.Multiply)
        .Register("divide", BuiltInFunctions.Divide)
        // Comparison
        .Register("equals", BuiltInFunctions.Equals)
        .Register("not_equals", BuiltInFunctions.NotEquals)
        .Register("greater_than", BuiltInFunctions.GreaterThan)
        .Register("less_than", BuiltInFunctions.LessThan)
        // Logical
        .Register("and", BuiltInFunctions.And)
        .Register("or", BuiltInFunctions.Or)
        .Register("not", BuiltInFunctions.Not)
        // String predicates
        .Register("contains", BuiltInFunctions.Contains)
        .Register("starts_with", BuiltInFunctions.StartsWith)
        .Register("ends_with", BuiltInFunctions.EndsWith)
        // Validation
        .Register("required", BuiltInFunctions.Required)
        .Register("email", BuiltInFunctions.Email)
        .Register("regex", BuiltInFunctions.RegexMatch)
        .Register("length", BuiltInFunctions.Length)
        .Register("numeric", BuiltInFunctions.Numeric)
        // Void
        .Register("openUrl", BuiltInFunctions.OpenUrl);
}
