using System.Reflection;
using DarkBot.Net.Api.Config;
using DarkBot.Net.Config;
using DarkBot.Net.Plugins.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DarkBot.Net.Plugins;

public sealed class FeatureRegistry : IPluginRegistry
{
    private readonly FeatureActivator _activator;
    private readonly ILogger<FeatureRegistry> _logger;
    private readonly string _pluginsPath;
    private readonly object _sync = new();
    private readonly List<PluginDescriptor> _plugins = [];
    private readonly List<LoadedFeature> _features = [];
    private string? _activeModuleId;

    public FeatureRegistry(
        FeatureActivator activator,
        IOptions<PluginOptions> options,
        ILogger<FeatureRegistry> logger)
    {
        _activator = activator;
        _logger = logger;
        _pluginsPath = Path.GetFullPath(options.Value.PluginsPath);
    }

    public event Action? Changed;

    public IReadOnlyList<LoadedFeature> Features
    {
        get
        {
            lock (_sync)
                return _features.ToList();
        }
    }

    public IReadOnlyList<PluginDescriptor> Plugins
    {
        get
        {
            lock (_sync)
                return _plugins.ToList();
        }
    }

    public string? ActiveModuleId
    {
        get
        {
            lock (_sync)
                return _activeModuleId;
        }
    }

    public IModule? ActiveModule
    {
        get
        {
            lock (_sync)
            {
                if (_activeModuleId is null)
                    return null;

                return GetInstance(_activeModuleId) as IModule;
            }
        }
    }

    public IReadOnlyList<IBehavior> EnabledBehaviors
    {
        get
        {
            lock (_sync)
                return _features
                    .Where(f => f.Enabled && f.Descriptor.IsBehavior && f.Instance is IBehavior)
                    .Select(f => (IBehavior)f.Instance!)
                    .ToList();
        }
    }

    public void LoadAll()
    {
        lock (_sync)
        {
            UnloadAllInternal();
            if (!Directory.Exists(_pluginsPath))
            {
                Directory.CreateDirectory(_pluginsPath);
                _logger.LogInformation("Created plugins folder at {Path}", _pluginsPath);
                return;
            }

            foreach (var dll in Directory.EnumerateFiles(_pluginsPath, "*.dll"))
                LoadPluginInternal(dll);

            EnsureDefaultActiveModule();
            Changed?.Invoke();
            _logger.LogInformation("Loaded {PluginCount} plugins, {FeatureCount} features",
                _plugins.Count, _features.Count);
        }
    }

    public void Reload()
    {
        _logger.LogInformation("Reloading plugins from {Path}", _pluginsPath);
        LoadAll();
    }

    public void SetEnabled(string featureId, bool enabled)
    {
        lock (_sync)
        {
            var feature = _features.FirstOrDefault(f => f.Descriptor.Id == featureId);
            if (feature is null)
                return;

            if (feature.Enabled == enabled)
                return;

            feature.Enabled = enabled;
            if (!enabled)
                feature.Instance = null;
            else if (feature.Instance is null)
                TryLoadInstance(feature);

            Changed?.Invoke();
        }
    }

    public void SetActiveModule(string? featureId)
    {
        lock (_sync)
        {
            if (featureId is not null &&
                _features.All(f => f.Descriptor.Id != featureId || !f.Descriptor.IsModule))
                return;

            _activeModuleId = featureId;
            if (featureId is not null)
            {
                var feature = _features.First(f => f.Descriptor.Id == featureId);
                feature.Enabled = true;
                if (feature.Instance is null)
                    TryLoadInstance(feature);
            }

            Changed?.Invoke();
        }
    }

    public object? GetInstance(string featureId)
    {
        lock (_sync)
        {
            var feature = _features.FirstOrDefault(f => f.Descriptor.Id == featureId);
            if (feature is null || !feature.Enabled)
                return null;

            if (feature.Instance is null)
                TryLoadInstance(feature);

            return feature.Instance;
        }
    }

    private void LoadPluginInternal(string pluginPath)
    {
        try
        {
            var context = new PluginLoadContext(pluginPath);
            var assembly = context.LoadPluginAssembly();
            var pluginName = Path.GetFileNameWithoutExtension(pluginPath);
            var features = DiscoverFeatures(assembly, pluginName, pluginPath);

            if (features.Count == 0)
            {
                context.Unload();
                _logger.LogWarning("No [Feature] types in {Plugin}", pluginPath);
                return;
            }

            _plugins.Add(new PluginDescriptor
            {
                Name = pluginName,
                Path = pluginPath,
                Assembly = assembly,
                Context = context,
                Features = features
            });

            foreach (var descriptor in features)
            {
                _features.Add(new LoadedFeature
                {
                    Descriptor = descriptor,
                    Enabled = descriptor.EnabledByDefault || descriptor.IsBehavior
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin {Plugin}", pluginPath);
        }
    }

    private List<FeatureDescriptor> DiscoverFeatures(Assembly assembly, string pluginName, string pluginPath)
    {
        var result = new List<FeatureDescriptor>();
        foreach (var type in assembly.GetExportedTypes())
        {
            var attr = type.GetCustomAttribute<FeatureAttribute>();
            if (attr is null || type.IsAbstract)
                continue;

            result.Add(new FeatureDescriptor
            {
                Id = type.FullName ?? type.Name,
                Name = attr.Name,
                Description = attr.Description,
                PluginName = pluginName,
                PluginPath = pluginPath,
                FeatureType = type,
                EnabledByDefault = attr.EnabledByDefault
            });
        }

        return result;
    }

    private void TryLoadInstance(LoadedFeature feature)
    {
        try
        {
            var instance = _activator.CreateInstance(feature.Descriptor.FeatureType);
            feature.Instance = instance;
            feature.LoadError = null;
            ApplyDefaultConfig(instance);
        }
        catch (Exception ex)
        {
            feature.LoadError = ex.Message;
            feature.Instance = null;
            _logger.LogError(ex, "Failed to instantiate {Feature}", feature.Descriptor.Id);
        }
    }

    private static void ApplyDefaultConfig(object instance)
    {
        var configurable = instance.GetType()
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConfigurable<>));

        if (configurable is null)
            return;

        var configType = configurable.GetGenericArguments()[0];
        var configValue = Activator.CreateInstance(configType);
        if (configValue is null)
            return;

        var settingType = typeof(ConfigSetting<>).MakeGenericType(configType);
        var setting = Activator.CreateInstance(settingType, configValue);
        var method = configurable.GetMethod(nameof(IConfigurable<object>.SetConfig));
        method?.Invoke(instance, [setting]);
    }

    private void EnsureDefaultActiveModule()
    {
        if (_activeModuleId is not null &&
            _features.Any(f => f.Descriptor.Id == _activeModuleId && f.Descriptor.IsModule))
            return;

        var firstModule = _features.FirstOrDefault(f => f.Descriptor.IsModule);
        _activeModuleId = firstModule?.Descriptor.Id;
        if (firstModule is not null)
        {
            firstModule.Enabled = true;
            TryLoadInstance(firstModule);
        }
    }

    private void UnloadAllInternal()
    {
        foreach (var feature in _features)
            feature.Instance = null;

        _features.Clear();
        _plugins.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

public interface IPluginRegistry
{
    IReadOnlyList<LoadedFeature> Features { get; }
    IReadOnlyList<PluginDescriptor> Plugins { get; }
    string? ActiveModuleId { get; }
    IModule? ActiveModule { get; }
    IReadOnlyList<IBehavior> EnabledBehaviors { get; }
    event Action? Changed;
    void LoadAll();
    void Reload();
    void SetEnabled(string featureId, bool enabled);
    void SetActiveModule(string? featureId);
    object? GetInstance(string featureId);
}
