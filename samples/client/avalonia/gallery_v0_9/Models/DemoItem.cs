using A2Ui.Core.Messages;

namespace A2Ui.Avalonia.Gallery.Models;

/// <summary>A single gallery example with its A2UI messages.</summary>
public sealed record DemoItem(
    string Id,
    string Title,
    string Filename,
    string Description,
    IReadOnlyList<A2UiMessage> Messages,
    bool IsBasic);
