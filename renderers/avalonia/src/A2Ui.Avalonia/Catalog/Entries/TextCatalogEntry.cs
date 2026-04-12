using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Media;

namespace A2Ui.Avalonia.Catalog;

/// <summary>
/// A2UI "Text" → Avalonia TextBlock.
/// Variants: h1, h2, h3, h4, h5, body (default), caption.
/// </summary>
public sealed class TextCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Text";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (TextComponent)component;
        var tb = new TextBlock
        {
            Text = context.Resolve(component.Text) ?? string.Empty,
            TextWrapping = TextWrapping.Wrap,
        };

        ApplyVariant(tb, typed.Variant);
        return tb;
    }

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (TextComponent)component;
        if (existing is not TextBlock tb)
        {
            return false;
        }

        tb.Text = context.Resolve(component.Text) ?? string.Empty;
        ApplyVariant(tb, typed.Variant);
        return true;
    }

    private static void ApplyVariant(TextBlock tb, string? variant)
    {
        tb.Classes.Clear();
        tb.Classes.Add(
            variant switch
            {
                "h1" => "Heading1",
                "h2" => "Heading2",
                "h3" => "Heading3",
                "h4" => "Heading4",
                "h5" => "Heading5",
                "caption" => "Caption",
                _ => "Body",
            }
        );
    }
}
