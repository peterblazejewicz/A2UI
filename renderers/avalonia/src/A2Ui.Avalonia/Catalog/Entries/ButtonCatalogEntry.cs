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
            // surfaceId is always resolved by RenderContext, not the caller
            btn.Click += (_, _) => ctx.FireUserAction(string.Empty, eventName);
        }

        return btn;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false; // recreate for simplicity

    private static void ApplyVariant(Button btn, string? variant)
    {
        btn.Classes.Clear();
        if (variant is "primary") btn.Classes.Add("accent");
        else if (variant is "danger") btn.Classes.Add("danger");
    }
}