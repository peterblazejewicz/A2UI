using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AgUi.Protocol.DependencyInjection;

/// <summary>
/// Extension methods for registering <c>AgUi.Protocol</c> services with a
/// <see cref="IServiceCollection"/>.
/// </summary>
public static class AgUiProtocolServiceCollectionExtensions
{
    /// <summary>
    /// Register AG-UI transport primitives.
    /// <see cref="ToolCallArgsAccumulator"/> is registered as transient because each
    /// accumulator instance is tied to a single tool-call stream.
    /// <see cref="Transport.SseEventParser"/> is a <see langword="static"/> class and
    /// needs no registration.
    /// </summary>
    /// <param name="services">The DI service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddAgUiProtocol(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddTransient<ToolCallArgsAccumulator>();
        return services;
    }
}
