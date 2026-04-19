using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(A2Ui.Avalonia.Tests.TestApp))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerAssembly)]

namespace A2Ui.Avalonia.Tests;

/// <summary>
/// Headless Avalonia application for UI tests.
/// Required by Avalonia.Headless.XUnit.
/// </summary>
public sealed class TestApp : Application
{
    public override void Initialize() => this.Styles.Add(new FluentTheme());

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
