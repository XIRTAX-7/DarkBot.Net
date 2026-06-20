using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Plugins;

/// <summary>Loads plugins on startup and optionally watches the plugins folder for hot-reload.</summary>
public sealed class PluginHostedService : IHostedService, IDisposable
{
    private readonly FeatureRegistry _registry;
    private readonly PluginOptions _options;
    private readonly ILogger<PluginHostedService> _logger;
    private FileSystemWatcher? _watcher;
    private Timer? _reloadDebounce;

    public PluginHostedService(
        FeatureRegistry registry,
        IOptions<PluginOptions> options,
        ILogger<PluginHostedService> logger)
    {
        _registry = registry;
        _options = options.Value;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _registry.LoadAll();
        if (_options.EnableHotReload)
            StartWatcher();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher?.Dispose();
        _reloadDebounce?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _reloadDebounce?.Dispose();
    }

    private void StartWatcher()
    {
        var path = Path.GetFullPath(_options.PluginsPath);
        if (!Directory.Exists(path))
            return;

        _watcher = new FileSystemWatcher(path, "*.dll")
        {
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnPluginFolderChanged;
        _watcher.Created += OnPluginFolderChanged;
        _watcher.Deleted += OnPluginFolderChanged;
        _watcher.Renamed += OnPluginFolderChanged;
        _logger.LogInformation("Plugin hot-reload watching {Path}", path);
    }

    private void OnPluginFolderChanged(object sender, FileSystemEventArgs e)
    {
        _reloadDebounce?.Dispose();
        _reloadDebounce = new Timer(_ =>
        {
            try
            {
                _registry.Reload();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin hot-reload failed");
            }
        }, null, 500, Timeout.Infinite);
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDarkBotPlugins(this IServiceCollection services)
    {
        services.AddOptions<PluginOptions>();

        services.AddSingleton<FeatureActivator>();
        services.AddSingleton<FeatureRegistry>();
        services.AddSingleton<IPluginRegistry>(sp => sp.GetRequiredService<FeatureRegistry>());
        services.AddSingleton<PluginHostedService>();
        services.AddHostedService(sp => sp.GetRequiredService<PluginHostedService>());

        return services;
    }
}
