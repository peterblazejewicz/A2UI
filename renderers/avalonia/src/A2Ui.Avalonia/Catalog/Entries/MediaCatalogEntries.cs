using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace A2Ui.Avalonia.Catalog;

/// <summary>A2UI "Image" → Avalonia Image. Loads from URL asynchronously.</summary>
public sealed class ImageCatalogEntry : ICatalogEntry
{
    private readonly ILogger<ImageCatalogEntry> _logger;

    public ImageCatalogEntry(ILogger<ImageCatalogEntry>? logger = null)
    {
        this._logger = logger ?? NullLogger<ImageCatalogEntry>.Instance;
    }

    public string ComponentType => "Image";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (ImageComponent)component;
        var stretch = typed.Fit switch
        {
            "cover" => Stretch.UniformToFill,
            "fill" => Stretch.Fill,
            "none" => Stretch.None,
            "scaleDown" => Stretch.Uniform,
            _ => Stretch.Uniform, // "contain" or default
        };
        var img = new Image { Stretch = stretch };
        string? url = context.Resolve(typed.Url);
        if (url is not null)
        {
            img.Tag = url;
            _ = this.LoadImageAsync(img, url, context.SurfaceCancellation);
        }
        return img;
    }

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (ImageComponent)component;
        if (existing is not Image img)
        {
            return false;
        }

        string? url = context.Resolve(typed.Url);
        if (url is null)
        {
            return true; // no URL yet — keep existing control as-is
        }

        // If the image already has a source and the URL tag matches, skip reload.
        if (img.Source is not null && img.Tag as string == url)
        {
            return true;
        }

        // URL changed or source not yet loaded — reload.
        img.Tag = url;
        _ = this.LoadImageAsync(img, url, context.SurfaceCancellation);
        return true;
    }

    private static readonly HttpClient s_http = new() { Timeout = TimeSpan.FromSeconds(30) };

    /// <summary>Maximum image download size (10 MB) to avoid unbounded memory allocation.</summary>
    private const int MaxImageBytes = 10 * 1024 * 1024;

    private async Task LoadImageAsync(Image img, string url, CancellationToken ct)
    {
        try
        {
            // Download to byte array — Avalonia's Bitmap constructor needs a seekable stream.
            byte[] data = await s_http.GetByteArrayAsync(url, ct).ConfigureAwait(false);

            if (data.Length > MaxImageBytes)
            {
                MediaLog.ImageExceedsSizeLimit(this._logger, url, MaxImageBytes / (1024 * 1024), data.Length);
                return;
            }

            using var ms = new MemoryStream(data);
            var bitmap = new Bitmap(ms);

            // Dispatcher.UIThread.InvokeAsync returns DispatcherOperation, not Task —
            // ConfigureAwait is not applicable.
#pragma warning disable CA2007
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Guard against out-of-order completion: another Update() may have
                // changed the target URL while this download was in flight.
                if (img.Tag as string != url)
                {
                    bitmap.Dispose();
                    return;
                }

                var old = img.Source as Bitmap;
                img.Source = bitmap;
                old?.Dispose();
            });
#pragma warning restore CA2007
        }
        catch (OperationCanceledException)
        {
            // Intentionally cancelled — not an error.
        }
        catch (Exception ex)
        {
            MediaLog.FailedToLoadImage(this._logger, url, ex);
        }
    }
}

/// <summary>A2UI "Table" → Avalonia DataGrid (extension, not in v0.9 spec).</summary>
public sealed class TableCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Table";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (TableComponent)component;
        var grid = new DataGrid { CanUserReorderColumns = true, IsReadOnly = true };
        if (typed.Columns is { } cols)
        {
            foreach (var col in cols)
            {
                grid.Columns.Add(new DataGridTextColumn { Header = col.Header, Binding = new Binding(col.Field) });
            }
        }
        return grid;
    }

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var typed = (TableComponent)component;
        if (existing is not DataGrid grid)
        {
            return false;
        }

        if (typed.Columns is not { } cols || cols.Length != grid.Columns.Count)
        {
            return false;
        }

        for (int i = 0; i < cols.Length; i++)
        {
            if (grid.Columns[i] is DataGridTextColumn col)
            {
                col.Header = cols[i].Header;
            }
        }

        return true;
    }
}

/// <summary>A2UI "Surface" → root container (extension, not in v0.9 spec).</summary>
public sealed class SurfaceCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Surface";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var panel = new StackPanel { Spacing = 12 };
        foreach (var child in context.RenderChildren(component.Id))
        {
            panel.Children.Add(child);
        }

        return panel;
    }

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        if (existing is not StackPanel panel)
        {
            return false;
        }

        panel.Children.Clear();
        foreach (var child in context.RenderChildren(component.Id))
        {
            panel.Children.Add(child);
        }

        return true;
    }
}

internal static partial class MediaLog
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Image at '{Url}' exceeds {MaxMb} MB limit ({ActualBytes} bytes), skipping"
    )]
    public static partial void ImageExceedsSizeLimit(ILogger logger, string url, int maxMb, int actualBytes);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to load image from '{Url}'")]
    public static partial void FailedToLoadImage(ILogger logger, string url, Exception exception);
}
