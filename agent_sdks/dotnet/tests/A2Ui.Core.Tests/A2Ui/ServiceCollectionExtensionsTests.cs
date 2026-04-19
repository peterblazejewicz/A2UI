using System;
using A2Ui.Core.DependencyInjection;
using A2Ui.Core.Surfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace A2Ui.Core.Tests.A2Ui;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddA2UiCore_WithoutConfigure_ResolvesSurfaceManagerWithDefaultOptions()
    {
        var services = new ServiceCollection();
        services.AddA2UiCore();

        using var sp = services.BuildServiceProvider();
        var sm = sp.GetRequiredService<SurfaceManager>();
        var opts = sp.GetRequiredService<IOptions<SurfaceManagerOptions>>();

        Assert.NotNull(sm);
        Assert.False(opts.Value.StrictMode);
    }

    [Fact]
    public void AddA2UiCore_WithConfigure_PropagatesStrictModeIntoSurfaceManager()
    {
        var services = new ServiceCollection();
        services.AddA2UiCore(opts => opts.StrictMode = true);

        using var sp = services.BuildServiceProvider();
        var sm = sp.GetRequiredService<SurfaceManager>();
        var opts = sp.GetRequiredService<IOptions<SurfaceManagerOptions>>();

        Assert.True(opts.Value.StrictMode);
        // StrictMode is internal policy; surface its effect via a strict-mode throw.
        // Duplicate delete of an unknown surface throws in strict mode.
        Assert.Throws<Messages.A2UiMessageValidationException>(() =>
            sm.Process(
                new Messages.A2UiMessage
                {
                    Version = "v0.9",
                    DeleteSurface = new Messages.DeleteSurface { SurfaceId = "ghost" },
                }
            )
        );
    }

    [Fact]
    public void AddA2UiCore_SurfaceManagerIsSingleton()
    {
        var services = new ServiceCollection();
        services.AddA2UiCore();

        using var sp = services.BuildServiceProvider();
        var first = sp.GetRequiredService<SurfaceManager>();
        var second = sp.GetRequiredService<SurfaceManager>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddA2UiCore_NullServices_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => A2UiCoreServiceCollectionExtensions.AddA2UiCore(null!));
    }
}
