namespace A2Ui.Core.Messages;

/// <summary>
/// Thrown when an A2UI message envelope violates protocol constraints
/// (e.g., missing version, zero or multiple operations).
/// </summary>
public sealed class A2UiMessageValidationException : Exception
{
    public A2UiMessageValidationException() { }

    public A2UiMessageValidationException(string message)
        : base(message) { }

    public A2UiMessageValidationException(string message, Exception innerException)
        : base(message, innerException) { }

    public A2UiMessageValidationException(IReadOnlyList<string> violations)
        : base(string.Join("; ", violations))
    {
        Violations = violations;
    }

    /// <summary>Structured list of individual constraint violations.</summary>
    public IReadOnlyList<string> Violations { get; } = [];
}
