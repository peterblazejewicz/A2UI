using A2Ui.Avalonia.Shell.ViewModels;
using A2Ui.Avalonia.Shell.Views;
using Avalonia;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace A2Ui.Avalonia.Shell;

public sealed partial class App : Application
{
    // Set by Program.BuildAvaloniaApp() before OnFrameworkInitializationCompleted runs.
    public static IServiceProvider Services { get; set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (
            ApplicationLifetime
            is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
        )
        {
            var window = Services.GetRequiredService<ShellWindow>();
            window.DataContext = Services.GetRequiredService<ShellViewModel>();
            desktop.MainWindow = window;

            desktop.ShutdownRequested += (_, _) =>
            {
                (Services as IDisposable)?.Dispose();
            };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
