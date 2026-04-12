using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests;

public sealed class DetachFromParentTests
{
    [AvaloniaFact]
    public void DetachFromParent_Panel_RemovesChildFromPanel()
    {
        var panel = new StackPanel();
        var child = new TextBlock { Text = "Hello" };
        panel.Children.Add(child);

        child.Parent.Should().Be(panel);

        RenderContext.DetachFromParent(child);

        child.Parent.Should().BeNull();
        panel.Children.Should().BeEmpty();
    }

    [AvaloniaFact]
    public void DetachFromParent_ContentControl_ClearsContent()
    {
        var cc = new ContentControl();
        var child = new TextBlock { Text = "Content" };
        cc.Content = child;

        // ContentControl sets child as logical child
        child.Parent.Should().Be(cc);

        RenderContext.DetachFromParent(child);

        child.Parent.Should().BeNull();
        cc.Content.Should().BeNull();
    }

    [AvaloniaFact]
    public void DetachFromParent_Decorator_ClearsChild()
    {
        var border = new Border();
        var child = new TextBlock { Text = "Bordered" };
        border.Child = child;

        child.Parent.Should().Be(border);

        RenderContext.DetachFromParent(child);

        child.Parent.Should().BeNull();
        border.Child.Should().BeNull();
    }

    [AvaloniaFact]
    public void DetachFromParent_NoParent_DoesNothing()
    {
        var orphan = new TextBlock { Text = "Orphan" };
        orphan.Parent.Should().BeNull();

        // Should not throw
        RenderContext.DetachFromParent(orphan);

        orphan.Parent.Should().BeNull();
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
        child.Parent.Should().Be(oldPanel);

        // Detach from old parent
        RenderContext.DetachFromParent(child);

        // Now adding to new parent should not throw
        newPanel.Children.Add(child);
        child.Parent.Should().Be(newPanel);
    }
}
