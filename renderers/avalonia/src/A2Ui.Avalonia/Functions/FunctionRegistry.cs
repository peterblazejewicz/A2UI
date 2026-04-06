using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace A2Ui.Avalonia.Functions;

/// <summary>
/// Immutable registry mapping function names to evaluation delegates.
/// Thread-safe for all operations after construction.
/// Use <see cref="FunctionRegistryBuilder"/> to construct instances.
/// </summary>
public sealed class FunctionRegistry : IFunctionRegistry
{
    private readonly FrozenDictionary<string, Func<IReadOnlyDictionary<string, string?>, string?>> _functions;

    internal FunctionRegistry(Dictionary<string, Func<IReadOnlyDictionary<string, string?>, string?>> functions)
    {
        _functions = functions.ToFrozenDictionary();
    }

    /// <inheritdoc />
    public string? Evaluate(string functionName, IReadOnlyDictionary<string, string?> resolvedArgs)
    {
        if (!_functions.TryGetValue(functionName, out var fn))
        {
            Trace.TraceWarning(
                $"[FunctionRegistry] Unknown function: {functionName}");
            return null;
        }

        try
        {
            return fn(resolvedArgs);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException
            or InvalidOperationException or JsonException
            or RegexMatchTimeoutException or OverflowException
            or KeyNotFoundException)
        {
            Trace.TraceWarning(
                $"[FunctionRegistry] Error evaluating '{functionName}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Create a registry pre-loaded with all A2UI built-in functions.
    /// </summary>
    public static FunctionRegistry CreateDefault() => new FunctionRegistryBuilder()
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
        .Register("openUrl", BuiltInFunctions.OpenUrl)
        .Build();
}

/// <summary>
/// Builder for constructing immutable <see cref="FunctionRegistry"/> instances.
/// </summary>
public sealed class FunctionRegistryBuilder
{
    private readonly Dictionary<string, Func<IReadOnlyDictionary<string, string?>, string?>> _functions = new();

    /// <summary>Register a named function. Returns this for chaining.</summary>
    public FunctionRegistryBuilder Register(string name,
        Func<IReadOnlyDictionary<string, string?>, string?> fn)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(fn);
        if (!_functions.TryAdd(name, fn))
            throw new ArgumentException($"Function '{name}' is already registered.", nameof(name));
        return this;
    }

    /// <summary>Build the immutable <see cref="FunctionRegistry"/>.</summary>
    public FunctionRegistry Build() => new(_functions);
}
