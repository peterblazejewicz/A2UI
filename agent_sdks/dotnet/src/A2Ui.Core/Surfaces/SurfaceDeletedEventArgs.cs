namespace A2Ui.Core.Surfaces;

/// <summary>Event arguments for the <see cref="SurfaceManager.SurfaceDeleted"/> event.</summary>
/// <param name="Surface">The deleted surface.</param>
public sealed record SurfaceDeletedEventArgs(Surface Surface);
