using System.Reflection;
using System.Runtime.Loader;

namespace DarkBot.Net.Plugins;

/// <summary>Collectible ALC per plugin DLL — shared host assemblies stay in default context.</summary>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    private static readonly string[] SharedAssemblies =
    [
        "DarkBot.Net.Api",
        "DarkBot.Net.Plugins.Abstractions",
        "DarkBot.Net.Config",
        "System.Runtime",
        "netstandard"
    ];

    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginPath;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _pluginPath = pluginPath;
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    public Assembly LoadPluginAssembly() => LoadFromAssemblyPath(_pluginPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (SharedAssemblies.Contains(assemblyName.Name, StringComparer.Ordinal))
            return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is not null ? LoadFromAssemblyPath(path) : null;
    }
}
