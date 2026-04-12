namespace A2Ui.Core.Surfaces;

/// <summary>Event arguments for the <see cref="SurfaceManager.SurfaceCreated"/> event.</summary>
/// <param name="Surface">The newly created surface.</param>
public sealed record SurfaceCreatedEventArgs(Surface Surface);
