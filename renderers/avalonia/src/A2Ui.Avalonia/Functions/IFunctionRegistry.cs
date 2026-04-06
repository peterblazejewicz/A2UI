namespace A2Ui.Avalonia.Functions;

/// <summary>
/// Evaluates named functions from A2UI catalog definitions.
/// Used by RenderContext to resolve FunctionCall DynamicValues.
/// </summary>
public interface IFunctionRegistry
{
    /// <summary>
    /// Evaluate a function by name with pre-resolved string arguments.
    /// Returns null if the function is unknown or evaluation fails.
    /// </summary>
    string? Evaluate(string functionName, IReadOnlyDictionary<string, string?> resolvedArgs);
}
