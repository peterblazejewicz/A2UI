using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;

namespace A2Ui.Avalonia.Catalog;

public sealed class ButtonCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Button";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var btn = new Button
        {
            Content = component.Child is not null
                ? context.RenderChild(component.Child)
                : (object?)(context.Resolve(component.Text) ?? context.Resolve(component.Label) ?? string.Empty),
        };

        ApplyVariant(btn, component.Variant);

        // Evaluate check conditions — disable button when any check fails.
        if (component.Checks is { Length: > 0 })
        {
            bool allPass = CheckHelper.AllChecksPassing(component, context);
            btn.IsEnabled = allPass;
            if (!allPass)
            {
                string? failedMessage = CheckHelper.FirstFailingMessage(component, context);
                if (failedMessage is not null)
                {
                    ToolTip.SetTip(btn, failedMessage);
                }
            }
        }

        if (component.Action?.Event is { } actionEvent)
        {
            string eventName = actionEvent.Name;
            var contextSpec = actionEvent.Context;
            string componentId = component.Id;
            btn.Click += (_, _) =>
            {
                object? payload = ResolveContext(contextSpec, context);
                context.FireUserAction(eventName, payload, componentId);
            };
        }

        return btn;
    }

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context) => false; // recreate for simplicity

    /// <summary>
    /// Resolve the action event context dictionary at invocation time.
    /// Each value is a DynamicValue that may reference the data model.
    /// </summary>
    private static Dictionary<string, string?>? ResolveContext(
        Dictionary<string, DynamicValue>? contextSpec,
        IRenderContext ctx
    )
    {
        if (contextSpec is null || contextSpec.Count == 0)
        {
            return null;
        }

        var resolved = new Dictionary<string, string?>(contextSpec.Count);
        foreach (var (key, dynVal) in contextSpec)
        {
            resolved[key] = ctx.Resolve(dynVal);
        }

        return resolved;
    }

    private static void ApplyVariant(Button btn, string? variant)
    {
        btn.Classes.Clear();
        if (variant is "primary")
        {
            btn.Classes.Add("accent");
        }
        else if (variant is "danger")
        {
            btn.Classes.Add("danger");
        }
        else if (variant is "borderless")
        {
            btn.Classes.Add("borderless");
        }
    }
}
