using System.Diagnostics;

namespace A2Ui.Avalonia;

/// <summary>
/// Centralized <see cref="ActivitySource"/>s for the A2UI Avalonia renderer.
/// Two sources: <c>A2Ui.Avalonia.Renderer</c> (used by <see cref="A2UiRenderer"/>)
/// and <c>A2Ui.Avalonia.Bridge</c> (reserved for future <c>AgentEventBridge</c>
/// instrumentation in Slice D).
/// </summary>
internal static class Diagnostics
{
    public static readonly ActivitySource RendererSource = new(
        name: "A2Ui.Avalonia.Renderer",
        version: typeof(Diagnostics).Assembly.GetName().Version?.ToString() ?? "0.0.0"
    );

    public static readonly ActivitySource BridgeSource = new(
        name: "A2Ui.Avalonia.Bridge",
        version: typeof(Diagnostics).Assembly.GetName().Version?.ToString() ?? "0.0.0"
    );
}
