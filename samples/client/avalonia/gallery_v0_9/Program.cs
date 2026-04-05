using A2Ui.Avalonia.Gallery.ViewModels;
using A2Ui.Core;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace A2Ui.Avalonia.Gallery;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        App.Services = services.BuildServiceProvider();

        return AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .WithInterFont()
                         .LogToTrace();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<SurfaceManager>();
        services.AddSingleton<GalleryViewModel>();
    }
}
