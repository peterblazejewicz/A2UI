using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;

namespace A2Ui.Avalonia.Catalog;

public sealed class ButtonCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Button";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var btn = new Button
        {
            Content = c.Child is not null
                ? ctx.RenderChild(c.Child)
                : (object?)(ctx.Resolve(c.Text) ?? ctx.Resolve(c.Label) ?? string.Empty),
        };

        ApplyVariant(btn, c.Variant);

        if (c.Action?.Event is { } actionEvent)
        {
            string eventName = actionEvent.Name;
            var contextSpec = actionEvent.Context;
            string componentId = c.Id;
            btn.Click += (_, _) =>
            {
                object? payload = ResolveContext(contextSpec, ctx);
                ctx.FireUserAction(eventName, payload, componentId);
            };
        }

        return btn;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false; // recreate for simplicity

    /// <summary>
    /// Resolve the action event context dictionary at invocation time.
    /// Each value is a DynamicValue that may reference the data model.
    /// </summary>
    private static Dictionary<string, string?>? ResolveContext(
        Dictionary<string, DynamicValue>? contextSpec, IRenderContext ctx)
    {
        if (contextSpec is null || contextSpec.Count == 0)
            return null;

        var resolved = new Dictionary<string, string?>(contextSpec.Count);
        foreach (var (key, dynVal) in contextSpec)
            resolved[key] = ctx.Resolve(dynVal);
        return resolved;
    }

    private static void ApplyVariant(Button btn, string? variant)
    {
        btn.Classes.Clear();
        if (variant is "primary") btn.Classes.Add("accent");
        else if (variant is "danger") btn.Classes.Add("danger");
    }
}