using A2Ui.Avalonia.Catalog;
using A2Ui.Avalonia.Shell.Models;
using A2Ui.Avalonia.Shell.Services;
using A2Ui.Avalonia.Shell.ViewModels;
using A2Ui.Avalonia.Shell.Views;
using A2Ui.Core;
using A2Ui.Core.Surfaces;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        // Eagerly construct RequestSummaryLogger so its ActivityListener
        // subscribes before the first A2A request is sent. The singleton's
        // lifetime then follows the service provider.
        _ = App.Services.GetRequiredService<RequestSummaryLogger>();

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

        services.AddSingleton(sp => new SurfaceManager(sp.GetRequiredService<ILoggerFactory>()));

        services.AddSingleton(sp => CatalogRegistry.CreateDefault(sp.GetRequiredService<ILoggerFactory>()));

        // Raw HTTP request/response logging handler. Transient lifetime is
        // required by AddHttpMessageHandler — one handler instance per
        // HttpClient built by the factory. Complements A2AAgentClient's
        // A2A-semantic logging: this layer captures network-level details
        // (method, URL, status, bytes, duration) regardless of the caller.
        services.AddTransient<LoggingHttpMessageHandler>();

        // Typed HttpClient for the A2A agent. The A2UI extension version is configured
        // here (not inside A2AAgentClient's constructor) so the wire contract is
        // declarative and visible at the DI boundary. See RESTAURANT_DEMO_PORT_PLAN.md
        // Fix #2 — the Shell requests v0.9 A2UI messages from the agent.
        services
            .AddHttpClient<IA2AClient, A2AAgentClient>(http =>
            {
                http.DefaultRequestHeaders.TryAddWithoutValidation(
                    A2AAgentClient.A2UiExtensionHeader,
                    A2AAgentClient.A2UiExtensionUri
                );
            })
            .AddHttpMessageHandler<LoggingHttpMessageHandler>();

        // ActivityListener that projects A2A.SendMessage Activity stop events
        // into a single Information-level RequestSummary log line. Registered
        // as singleton and eager-constructed in BuildAvaloniaApp() so the
        // listener subscribes before the first request.
        services.AddSingleton<RequestSummaryLogger>();

        services.AddSingleton<ShellViewModel>();
        services.AddTransient<ShellWindow>();
    }
}
