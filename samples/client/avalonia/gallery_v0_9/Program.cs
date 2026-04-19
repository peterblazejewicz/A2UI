using A2Ui.Avalonia.DependencyInjection;
using A2Ui.Avalonia.Gallery.Services;
using A2Ui.Avalonia.Gallery.ViewModels;
using A2Ui.Avalonia.Gallery.Views;
using A2Ui.Core.DependencyInjection;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace A2Ui.Avalonia.Gallery;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
            )
            .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, "logs", "gallery-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7
            )
            .Enrich.FromLogContext()
            .CreateLogger();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSerilog(dispose: true));
        ConfigureServices(services);
        App.Services = services.BuildServiceProvider();

        return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddA2UiCore();
        services.AddA2UiAvalonia();
        services.AddSingleton<GalleryDataLoader>();
        services.AddSingleton<GalleryViewModel>();
        services.AddTransient<GalleryWindow>();
    }
}
