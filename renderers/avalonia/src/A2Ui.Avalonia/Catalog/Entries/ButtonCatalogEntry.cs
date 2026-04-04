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
                : (object?)(ctx.Resolve(c.Text) ?? c.Label ?? string.Empty),
        };

        ApplyVariant(btn, c.Variant);

        if (c.Action?.Event is { } actionEvent)
        {
            string surfaceId = c.Parent ?? string.Empty; // resolved by renderer context
            string eventName = actionEvent.Name;
            btn.Click += (_, _) => ctx.FireUserAction(surfaceId, eventName);
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