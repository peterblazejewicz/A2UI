using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;

namespace A2Ui.Avalonia.Tests;

internal sealed class MockRenderContext(DataModel dataModel) : IRenderContext
{
    public Control? RenderChild(string? childId) => null;
    public IEnumerable<Control> RenderChildren(string parentId) => [];
    public void FireUserAction(string eventName, object? payload = null, string? componentId = null) { }
    public string? Resolve(DynamicValue? value) => dataModel.Resolve(value);
    public void UpdateDataModel(string path, string? value) { }
    public double? GetComponentWeight(string componentId) => null;
    public ILogger? Logger => null;
}
