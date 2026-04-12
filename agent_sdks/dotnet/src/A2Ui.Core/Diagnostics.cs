using System.Diagnostics;

namespace A2Ui.Core;

/// <summary>
/// Centralized <see cref="ActivitySource"/> for the A2UI core SDK.
/// Used by <see cref="Surfaces.SurfaceManager"/> to produce per-message dispatch spans
/// that participate in the end-to-end Restaurant Shell trace tree.
/// </summary>
internal static class Diagnostics
{
    public static readonly ActivitySource Source = new(
        name: "A2Ui.Core",
        version: typeof(Diagnostics).Assembly.GetName().Version?.ToString() ?? "0.0.0"
    );
}
