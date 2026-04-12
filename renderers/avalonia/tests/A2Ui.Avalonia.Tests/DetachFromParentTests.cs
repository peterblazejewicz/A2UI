using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace A2Ui.Avalonia.Tests;

public sealed class DetachFromParentTests
{
    [AvaloniaFact]
    public void DetachFromParent_Panel_RemovesChildFromPanel()
    {
        var panel = new StackPanel();
        var child = new TextBlock { Text = "Hello" };
        panel.Children.Add(child);

        Assert.Equal(panel, child.Parent);

        RenderContext.DetachFromParent(child);

        Assert.Null(child.Parent);
        Assert.Empty(panel.Children);
    }

    [AvaloniaFact]
    public void DetachFromParent_ContentControl_ClearsContent()
    {
        var cc = new ContentControl();
        var child = new TextBlock { Text = "Content" };
        cc.Content = child;

        // ContentControl sets child as logical child
        Assert.Equal(cc, child.Parent);

        RenderContext.DetachFromParent(child);

        Assert.Null(child.Parent);
        Assert.Null(cc.Content);
    }

    [AvaloniaFact]
    public void DetachFromParent_Decorator_ClearsChild()
    {
        var border = new Border();
        var child = new TextBlock { Text = "Bordered" };
        border.Child = child;

        Assert.Equal(border, child.Parent);

        RenderContext.DetachFromParent(child);

        Assert.Null(child.Parent);
        Assert.Null(border.Child);
    }

    [AvaloniaFact]
    public void DetachFromParent_NoParent_DoesNothing()
    {
        var orphan = new TextBlock { Text = "Orphan" };
        Assert.Null(orphan.Parent);

        // Should not throw
        RenderContext.DetachFromParent(orphan);

        Assert.Null(orphan.Parent);
    }

    [AvaloniaFact]
    public void DetachFromParent_AllowsReparenting()
    {
        // This is the exact scenario that caused the crash:
        // child in old panel, need to add to new panel
        var oldPanel = new StackPanel();
        var newPanel = new StackPanel();
        var child = new TextBlock { Text = "Shared" };

        oldPanel.Children.Add(child);
        Assert.Equal(oldPanel, child.Parent);

        // Detach from old parent
        RenderContext.DetachFromParent(child);

        // Now adding to new parent should not throw
        newPanel.Children.Add(child);
        Assert.Equal(newPanel, child.Parent);
    }
}
