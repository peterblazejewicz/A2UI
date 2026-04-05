using Avalonia;
using Avalonia.Markup.Xaml;

namespace A2Ui.Avalonia.Gallery;

public sealed partial class App : Application
{
    public static IServiceProvider Services { get; set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.GalleryWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
