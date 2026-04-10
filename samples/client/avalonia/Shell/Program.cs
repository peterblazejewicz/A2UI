using A2Ui.Avalonia.Shell.Models;
using A2Ui.Avalonia.Shell.Services;
using A2Ui.Avalonia.Shell.ViewModels;
using A2Ui.Avalonia.Shell.Views;
using A2Ui.Core;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace A2Ui.Avalonia.Shell;

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
                Path.Combine(AppContext.BaseDirectory, "logs", "shell-.log"),
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
        // Agent configuration — default to Python restaurant_finder on localhost
        services.AddSingleton(
            new AgentConfig(
                ServerUrl: "http://localhost:10002",
                Title: "Restaurant Finder",
                Placeholder: "Ask about restaurants..."
            )
        );

        services.AddSingleton<SurfaceManager>();

        // Typed HttpClient for the A2A agent. The A2UI extension version is configured
        // here (not inside A2AAgentClient's constructor) so the wire contract is
        // declarative and visible at the DI boundary. See RESTAURANT_DEMO_PORT_PLAN.md
        // Fix #2 — the Shell requests v0.9 A2UI messages from the agent.
        services.AddHttpClient<IA2AClient, A2AAgentClient>(http =>
        {
            http.DefaultRequestHeaders.TryAddWithoutValidation(
                A2AAgentClient.A2UiExtensionHeader,
                A2AAgentClient.A2UiExtensionUri
            );
        });

        services.AddSingleton<ShellViewModel>();
        services.AddTransient<ShellWindow>();
    }
}
