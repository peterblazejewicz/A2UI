namespace A2Ui.Core.Messages;

/// <summary>
/// Thrown when an A2UI message envelope violates protocol constraints
/// (e.g., missing version, zero or multiple operations).
/// </summary>
public sealed class A2UiMessageValidationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="A2UiMessageValidationException"/> class.</summary>
    public A2UiMessageValidationException() { }

    /// <summary>Initializes a new instance with a message.</summary>
    /// <param name="message">The error message.</param>
    public A2UiMessageValidationException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance with a message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public A2UiMessageValidationException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>Initializes a new instance from a list of constraint violations.</summary>
    /// <param name="violations">Individual constraint violation descriptions.</param>
    public A2UiMessageValidationException(IReadOnlyList<string> violations)
        : base(string.Join("; ", violations))
    {
        Violations = violations;
    }

    /// <summary>Structured list of individual constraint violations.</summary>
    public IReadOnlyList<string> Violations { get; } = [];
}
