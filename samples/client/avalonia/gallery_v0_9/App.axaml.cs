using A2Ui.Avalonia.Gallery.ViewModels;
using A2Ui.Avalonia.Gallery.Views;
using Avalonia;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace A2Ui.Avalonia.Gallery;

public sealed partial class App : Application
{
    public static IServiceProvider Services { get; set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (
            ApplicationLifetime
            is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
        )
        {
            var window = Services.GetRequiredService<GalleryWindow>();
            window.DataContext = Services.GetRequiredService<GalleryViewModel>();
            desktop.MainWindow = window;

            desktop.ShutdownRequested += (_, _) =>
            {
                (Services as IDisposable)?.Dispose();
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
