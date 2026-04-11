using System.Diagnostics;

namespace A2Ui.Avalonia.Shell;

/// <summary>
/// Centralized <see cref="ActivitySource"/> for the Restaurant Shell's A2A client layer.
/// Used by <see cref="Services.A2AAgentClient"/> to produce the root span
/// (<c>A2A.SendMessage</c>, <see cref="ActivityKind.Client"/>) of the
/// end-to-end request trace tree.
/// </summary>
internal static class Diagnostics
{
    public static readonly ActivitySource Source = new(
        name: "A2Ui.Shell.A2AClient",
        version: typeof(Diagnostics).Assembly.GetName().Version?.ToString() ?? "0.0.0"
    );
}
