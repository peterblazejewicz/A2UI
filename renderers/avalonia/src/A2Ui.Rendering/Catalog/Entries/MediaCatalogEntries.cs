using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace A2Ui.Rendering.Catalog;

/// <summary>A2UI "Image" → Avalonia Image. Loads from URL asynchronously.</summary>
public sealed class ImageCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Image";

    public Control Create(A2UiComponent c, DataModel dm, IRenderContext ctx)
    {
        var img = new Image { Stretch = Avalonia.Media.Stretch.Uniform };
        string? url = ctx.Resolve(c.Url) ?? ctx.Resolve(c.Value);
        if (url is not null) LoadAsync(img, url);
        return img;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;

    private static async void LoadAsync(Image img, string url)
    {
        try
        {
            using var http = new System.Net.Http.HttpClient();
            await using var stream = await http.GetStreamAsync(url).ConfigureAwait(true);
            img.Source = new Bitmap(stream);
        }
        catch { /* silently fail — broken image stays empty */ }
    }
}

/// <summary>A2UI "Table" → Avalonia DataGrid.</summary>
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
                    Binding = new Avalonia.Data.Binding(col.Field),
                });
            }
        }
        return grid;
    }

    public bool Update(Control existing, A2UiComponent c, DataModel dm,
                       IRenderContext ctx) => false;
}

/// <summary>A2UI "Surface" → UserControl root container.</summary>
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