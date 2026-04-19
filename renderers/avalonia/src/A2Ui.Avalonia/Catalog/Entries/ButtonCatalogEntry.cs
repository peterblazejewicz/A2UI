using A2Ui.Core.Bindings;
using A2Ui.Core.Components;
using A2Ui.Core.Surfaces;
using Avalonia.Controls;

namespace A2Ui.Avalonia.Catalog.Entries;

/// <summary>A2UI "Button" → Avalonia Button with variant styling and action handling.</summary>
public sealed class ButtonCatalogEntry : ICatalogEntry
{
    /// <inheritdoc />
    public string ComponentType => "Button";

    /// <inheritdoc />
    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (ButtonComponent)component;
        var btn = new Button
        {
            Content = component.Child is not null
                ? context.RenderChild(component.Child)
                : (object?)(context.Resolve(component.Text) ?? context.Resolve(component.Label) ?? string.Empty),
        };

        ApplyVariant(btn, typed.Variant);

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

        if (typed.Action?.Event is { } actionEvent)
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

    /// <inheritdoc />
    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (ButtonComponent)component;
        if (existing is not Button btn)
        {
            return false;
        }

        btn.Content = component.Child is not null
            ? context.RenderChild(component.Child)
            : (object?)(context.Resolve(component.Text) ?? context.Resolve(component.Label) ?? string.Empty);

        ApplyVariant(btn, typed.Variant);

        if (component.Checks is { Length: > 0 })
        {
            bool allPass = CheckHelper.AllChecksPassing(component, context);
            btn.IsEnabled = allPass;
            string? failedMessage = allPass ? null : CheckHelper.FirstFailingMessage(component, context);
            ToolTip.SetTip(btn, failedMessage);
        }
        else
        {
            btn.IsEnabled = true;
            ToolTip.SetTip(btn, null);
        }

        return true;
    }

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
