using A2Ui.Rendering.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;

namespace A2Ui.Rendering.Tests;

internal sealed class MockRenderContext(DataModel dataModel) : IRenderContext
{
    public Control? RenderChild(string? childId) => null;
    public IEnumerable<Control> RenderChildren(string parentId) => [];
    public void FireUserAction(string surfaceId, string eventName, object? payload = null) { }
    public string? Resolve(DynamicValue? value) => dataModel.Resolve(value);
}