using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace A2Ui.Avalonia.Catalog;

/// <summary>A2UI "Image" → Avalonia Image. Loads from URL asynchronously.</summary>
public sealed class ImageCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Image";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var stretch = c.Fit switch
        {
            "cover"     => Stretch.UniformToFill,
            "fill"      => Stretch.Fill,
            "none"      => Stretch.None,
            "scaleDown" => Stretch.Uniform,
            _           => Stretch.Uniform, // "contain" or default
        };
        var img = new Image { Stretch = stretch };
        string? url = ctx.Resolve(c.Url) ?? ctx.Resolve(c.Value);
        if (url is not null)
            _ = LoadImageAsync(img, url); // fire-and-forget; errors handled inside
        return img;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;

    private static async Task LoadImageAsync(Image img, string url)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient();
            await using var stream = await http.GetStreamAsync(url).ConfigureAwait(true);
            img.Source = new Bitmap(stream);
        }
        catch
        {
            // Broken image URL — leave the Image control empty
        }
    }
}

/// <summary>A2UI "Table" → Avalonia DataGrid (extension, not in v0.9 spec).</summary>
public sealed class TableCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Table";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var grid = new DataGrid { CanUserReorderColumns = true, IsReadOnly = true };
        if (c.Columns is { } cols)
        {
            foreach (var col in cols)
            {
                grid.Columns.Add(new DataGridTextColumn
                {
                    Header  = col.Header,
                    Binding = new Binding(col.Field),
                });
            }
        }
        return grid;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "Surface" → root container (extension, not in v0.9 spec).</summary>
public sealed class SurfaceCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Surface";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var panel = new StackPanel { Spacing = 12 };
        foreach (var child in ctx.RenderChildren(c.Id))
            panel.Children.Add(child);
        return panel;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}
