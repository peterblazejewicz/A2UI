namespace A2Ui.Core.Surfaces;

/// <summary>Event arguments for the <see cref="SurfaceManager.DataModelUpdated"/> event.</summary>
/// <param name="Surface">The surface whose data model was updated.</param>
public sealed record DataModelUpdatedEventArgs(Surface Surface);
