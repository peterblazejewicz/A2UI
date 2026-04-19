namespace A2Ui.Core.Surfaces;

/// <summary>
/// Options controlling <see cref="SurfaceManager"/> behavior when incoming A2UI messages
/// violate spec-MUST constraints.
/// </summary>
/// <remarks>
/// <para>
/// By default <see cref="SurfaceManager"/> is lenient: duplicate <c>createSurface</c>,
/// operations on unknown surfaces, a missing <c>root</c> component after
/// <c>updateComponents</c>, and malformed data-model writes are logged as warnings and
/// processing continues. This matches how the Lit shell behaves and is appropriate for
/// production consumer apps that want to recover gracefully from flaky agent output.
/// </para>
/// <para>
/// Test suites, conformance harnesses, and CI-gated integrations typically prefer to fail
/// fast on protocol violations. Set <see cref="StrictMode"/> to <see langword="true"/> to
/// have <see cref="SurfaceManager"/> throw <see cref="Messages.A2UiMessageValidationException"/>
/// at each of the four leniency sites instead of returning null. The existing
/// <c>LoggerMessage</c> events still fire in strict mode — the log is useful context for
/// the message that preceded the throw.
/// </para>
/// </remarks>
public sealed record SurfaceManagerOptions
{
    /// <summary>
    /// When <see langword="true"/>, spec-MUST violations throw
    /// <see cref="Messages.A2UiMessageValidationException"/> instead of being logged and
    /// ignored. Default <see langword="false"/> preserves the lenient pre-options behavior.
    /// </summary>
    public bool StrictMode { get; init; }
}
