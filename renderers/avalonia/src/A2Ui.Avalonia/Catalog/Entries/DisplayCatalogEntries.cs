using A2Ui.Core;
using A2Ui.Core.Messages;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace A2Ui.Avalonia.Catalog;

/// <summary>A2UI "Icon" → TextBlock with Unicode icon mapping.</summary>
public sealed class IconCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Icon";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        string? name = context.Resolve(component.Name);
        return new TextBlock
        {
            Text = MapIconName(name),
            FontSize = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
    }

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        if (existing is not TextBlock tb)
        {
            return false;
        }

        tb.Text = MapIconName(context.Resolve(component.Name));
        return true;
    }

    private static string MapIconName(string? name) =>
        name switch
        {
            "add" => "\u2795",
            "check" => "\u2714",
            "close" => "\u2716",
            "delete" => "\uD83D\uDDD1",
            "edit" => "\u270F",
            "error" => "\u26A0",
            "favorite" => "\u2764",
            "home" => "\uD83C\uDFE0",
            "info" => "\u2139",
            "mail" => "\u2709",
            "menu" => "\u2630",
            "pause" => "\u23F8",
            "play" => "\u25B6",
            "search" => "\uD83D\uDD0D",
            "send" => "\u27A4",
            "settings" => "\u2699",
            "star" => "\u2B50",
            "warning" => "\u26A0",
            "arrowBack" => "\u2190",
            "arrowForward" => "\u2192",
            "refresh" => "\u21BB",
            "download" => "\u2B07",
            "upload" => "\u2B06",
            "visibility" => "\uD83D\uDC41",
            "visibilityOff" => "\uD83D\uDE48",
            "lock" => "\uD83D\uDD12",
            "lockOpen" => "\uD83D\uDD13",
            "person" => "\uD83D\uDC64",
            "notifications" => "\uD83D\uDD14",
            _ => "\u25A0", // default filled square
        };
}

/// <summary>A2UI "Divider" → Separator.</summary>
public sealed class DividerCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Divider";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        var separator = new Separator();
        ApplyAxis(separator, component.Axis);
        return separator;
    }

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context)
    {
        if (existing is not Separator sep)
        {
            return false;
        }

        ApplyAxis(sep, component.Axis);
        return true;
    }

    private static void ApplyAxis(Separator separator, string? axis)
    {
        if (axis is "vertical")
        {
            separator.Width = 1;
            separator.Height = double.NaN;
            separator.Margin = new Thickness(4, 0);
            separator.HorizontalAlignment = HorizontalAlignment.Center;
            separator.VerticalAlignment = VerticalAlignment.Stretch;
        }
        else
        {
            separator.Width = double.NaN;
            separator.Height = double.NaN;
            separator.Margin = new Thickness(0, 4);
            separator.HorizontalAlignment = HorizontalAlignment.Stretch;
            separator.VerticalAlignment = VerticalAlignment.Center;
        }
    }
}

/// <summary>A2UI "Video" → placeholder (Avalonia has no native video control).</summary>
public sealed class VideoCatalogEntry : ICatalogEntry
{
    public string ComponentType => "Video";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context) =>
        new TextBlock { Text = $"[Video: {context.Resolve(component.Url) ?? "no url"}]", Classes = { "Caption" } };

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context) => false;
}

/// <summary>A2UI "AudioPlayer" → placeholder (Avalonia has no native audio control).</summary>
public sealed class AudioPlayerCatalogEntry : ICatalogEntry
{
    public string ComponentType => "AudioPlayer";

    public Control Create(A2UiComponent component, DataModel dataModel, IRenderContext context) =>
        new TextBlock
        {
            Text = $"[AudioPlayer: {context.Resolve(component.Url) ?? "no url"}]",
            Classes = { "Caption" },
        };

    public bool Update(Control existing, A2UiComponent component, DataModel dataModel, IRenderContext context) => false;
}
