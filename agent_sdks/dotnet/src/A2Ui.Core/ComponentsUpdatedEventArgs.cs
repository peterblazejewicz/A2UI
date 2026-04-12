using A2Ui.Core.Messages;

namespace A2Ui.Core;

/// <summary>Event arguments for the <see cref="SurfaceManager.ComponentsUpdated"/> event.</summary>
/// <param name="Surface">The surface whose components were updated.</param>
/// <param name="Updated">The components that were added or replaced.</param>
public sealed record ComponentsUpdatedEventArgs(Surface Surface, A2UiComponent[] Updated);
