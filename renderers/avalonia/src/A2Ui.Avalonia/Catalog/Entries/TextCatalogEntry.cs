using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Layout;

namespace A2Ui.Avalonia.Catalog;

/// <summary>
/// A2UI "Text" → Avalonia TextBlock.
/// Variants: h1, h2, h3, body (default), caption, label.
/// </summary>
public sealed class TextCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Text";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var tb = new TextBlock
        {
            Text = ctx.Resolve(c.Text) ?? string.Empty,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        };

        ApplyVariant(tb, c.Variant);
        return tb;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        if (existing is not TextBlock tb) return false;
        tb.Text = ctx.Resolve(c.Text) ?? string.Empty;
        ApplyVariant(tb, c.Variant);
        return true;
    }

    private static void ApplyVariant(TextBlock tb, string? variant)
    {
        tb.Classes.Clear();
        tb.Classes.Add(variant switch
        {
            "h1"      => "Heading1",
            "h2"      => "Heading2",
            "h3"      => "Heading3",
            "caption" => "Caption",
            "label"   => "Label",
            _         => "Body",
        });
    }
}