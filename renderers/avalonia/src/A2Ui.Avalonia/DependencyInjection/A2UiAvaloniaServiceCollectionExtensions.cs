using A2Ui.Avalonia.Catalog;
using A2Ui.Avalonia.Functions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace A2Ui.Avalonia.DependencyInjection;

/// <summary>
/// Extension methods for registering <c>A2Ui.Avalonia</c> renderer services with a
/// <see cref="IServiceCollection"/>.
/// </summary>
public static class A2UiAvaloniaServiceCollectionExtensions
{
    /// <summary>
    /// Register the default catalog (<see cref="CatalogRegistry.CreateDefault(ILoggerFactory?)"/>)
    /// and function registry (<see cref="FunctionRegistry.CreateDefault(ILoggerFactory?)"/>) as
    /// singletons. Both resolve an optional <see cref="ILoggerFactory"/> from the container so
    /// catalog entries and function evaluation participate in the app's logging pipeline.
    /// </summary>
    /// <param name="services">The DI service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    /// <remarks>
    /// This does not register <c>A2UiRenderer</c> or the <c>A2UiSurface</c> control because
    /// both are constructed per-surface-host (the Avalonia control creates its own renderer
    /// to scope the control cache and subscriptions to its surface).
    /// </remarks>
    public static IServiceCollection AddA2UiAvalonia(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<CatalogRegistry>(sp => CatalogRegistry.CreateDefault(sp.GetService<ILoggerFactory>()));
        services.TryAddSingleton<IFunctionRegistry>(sp =>
            FunctionRegistry.CreateDefault(sp.GetService<ILoggerFactory>())
        );

        return services;
    }
}
