using System.Reflection;
using DarkBot.Net.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Plugins;

public sealed class PluginApi(IServiceProvider services) : IPluginApi
{
    public T Require<T>() where T : class =>
        services.GetRequiredService<T>();

    public T? Optional<T>() where T : class =>
        services.GetService<T>();

    public T RequireInstance<T>() where T : class =>
        services.GetRequiredService<T>();
}

public sealed class FeatureActivator(IServiceProvider services, ILogger<FeatureActivator> logger)
{
    private readonly PluginApi _pluginApi = new(services);

    public object CreateInstance(Type featureType)
    {
        var constructors = featureType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructors.Length == 0)
            return ActivatorUtilities.CreateInstance(services, featureType);

        var ctor = constructors
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var args = ctor.GetParameters()
            .Select(p => ResolveParameter(p.ParameterType))
            .ToArray();

        return ctor.Invoke(args);
    }

    private object? ResolveParameter(Type type)
    {
        if (type == typeof(IPluginApi))
            return _pluginApi;

        var service = services.GetService(type);
        if (service is not null)
            return service;

        logger.LogWarning("No DI service for {Type} — using default if possible", type.Name);
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
