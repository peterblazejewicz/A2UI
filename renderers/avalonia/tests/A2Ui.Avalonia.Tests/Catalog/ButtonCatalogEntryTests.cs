using A2Ui.Avalonia.Catalog;
using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.Logging;
using FluentAssertions;

namespace A2Ui.Avalonia.Tests.Catalog;

public sealed class ButtonCatalogEntryTests
{
    [AvaloniaFact]
    public void ButtonCatalogEntry_Create_WithAction_FiresEventOnClick()
    {
        var entry = new ButtonCatalogEntry();
        var dm = new DataModel();
        var ctx = new ActionCapturingRenderContext(dm);
        var component = new A2UiComponent
        {
            Id = "btn1", Component = "Button",
            Text = DynamicValue.FromString("Go"),
            Action = new ComponentAction
            {
                Event = new ActionEvent { Name = "go_clicked" },
            },
        };

        var control = entry.Create(component, dm, ctx);
        control.Should().BeOfType<Button>();
        var btn = (Button)control;

        // Simulate click via RaiseEvent
        btn.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

        ctx.FiredEvents.Should().ContainSingle();
        ctx.FiredEvents[0].EventName.Should().Be("go_clicked");
    }

    [AvaloniaFact]
    public void ButtonCatalogEntry_Create_WithActionContext_ResolvesPathsAtClickTime()
    {
        var entry = new ButtonCatalogEntry();
        var dm = new DataModel();
        var ctx = new ActionCapturingRenderContext(dm);

        var component = new A2UiComponent
        {
            Id = "btn2", Component = "Button",
            Text = DynamicValue.FromString("Submit"),
            Action = new ComponentAction
            {
                Event = new ActionEvent
                {
                    Name = "form_submitted",
                    Context = new Dictionary<string, DynamicValue>
                    {
                        ["user"] = DynamicValue.FromPath("/username"),
                        ["greeting"] = DynamicValue.FromString("hello"),
                    },
                },
            },
        };

        var control = entry.Create(component, dm, ctx);
        var btn = (Button)control;

        // Set data model values AFTER button creation (simulates user typing)
        dm.Apply(new UpdateDataModel
        {
            SurfaceId = "test",
            Path = "/username",
            Value = System.Text.Json.JsonSerializer.SerializeToElement("peter"),
        });

        // Click → context should resolve from current data model state
        btn.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

        ctx.FiredEvents.Should().ContainSingle();
        var payload = ctx.FiredEvents[0].Payload.Should().BeAssignableTo<Dictionary<string, string?>>().Subject;
        payload["user"].Should().Be("peter");
        payload["greeting"].Should().Be("hello");
    }

    [AvaloniaFact]
    public void ButtonCatalogEntry_Create_WithEmptyContext_PassesNullPayload()
    {
        var entry = new ButtonCatalogEntry();
        var dm = new DataModel();
        var ctx = new ActionCapturingRenderContext(dm);

        var component = new A2UiComponent
        {
            Id = "btn3", Component = "Button",
            Text = DynamicValue.FromString("Click"),
            Action = new ComponentAction
            {
                Event = new ActionEvent
                {
                    Name = "clicked",
                    Context = new Dictionary<string, DynamicValue>(),
                },
            },
        };

        var control = entry.Create(component, dm, ctx);
        var btn = (Button)control;
        btn.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));

        ctx.FiredEvents.Should().ContainSingle();
        ctx.FiredEvents[0].Payload.Should().BeNull();
    }

    [AvaloniaFact]
    public void ButtonCatalogEntry_Create_WithoutAction_DoesNotThrow()
    {
        var entry = new ButtonCatalogEntry();
        var dm = new DataModel();
        var ctx = new ActionCapturingRenderContext(dm);

        var component = new A2UiComponent
        {
            Id = "btn4", Component = "Button",
            Text = DynamicValue.FromString("Display Only"),
            // No Action set
        };

        var control = entry.Create(component, dm, ctx);
        control.Should().BeOfType<Button>();
        var btn = (Button)control;

        // Click should not throw or fire events
        btn.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        ctx.FiredEvents.Should().BeEmpty();
    }

    [AvaloniaFact]
    public void ButtonCatalogEntry_Create_PrimaryVariant_HasAccentClass()
    {
        var entry = new ButtonCatalogEntry();
        var dm = new DataModel();
        var ctx = new ActionCapturingRenderContext(dm);

        var component = new A2UiComponent
        {
            Id = "btn5", Component = "Button",
            Text = DynamicValue.FromString("Primary"),
            Variant = "primary",
        };

        var control = entry.Create(component, dm, ctx);
        control.Classes.Should().Contain("accent");
    }
}

/// <summary>
/// Render context that captures fired user actions for assertion.
/// </summary>
internal sealed class ActionCapturingRenderContext(DataModel dm) : IRenderContext
{
    public List<(string EventName, object? Payload, string? ComponentId)> FiredEvents { get; } = [];

    public Control? RenderChild(string? childId) => null;
    public IEnumerable<Control> RenderChildren(string parentId) => [];
    public void FireUserAction(string eventName, object? payload = null, string? componentId = null) =>
        FiredEvents.Add((eventName, payload, componentId));
    public string? Resolve(DynamicValue? value) => dm.Resolve(value);
    public void UpdateDataModel(string path, string? value) { }
    public double? GetComponentWeight(string componentId) => null;
    public ILogger? Logger => null;
}
