using System.Diagnostics;

namespace AgUi.Protocol;

/// <summary>
/// Centralized <see cref="ActivitySource"/> for the AG-UI protocol library.
/// Used by <see cref="ToolCallArgsAccumulator"/> (and, indirectly, the
/// Avalonia <c>AgentEventBridge</c> via its accumulator field) to produce
/// spans covering the AG-UI tool-call accumulation lifecycle. Off the
/// Restaurant Shell hot path — the Shell speaks A2A HTTP and never
/// exercises this source.
/// </summary>
internal static class Diagnostics
{
    public static readonly ActivitySource Source = new(
        name: "A2Ui.AgUi.Protocol",
        version: typeof(Diagnostics).Assembly.GetName().Version?.ToString() ?? "0.0.0"
    );
}
