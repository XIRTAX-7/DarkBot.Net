using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DarkBot.Net.Plugins;

namespace DarkBot.Net.Ui.ViewModels;

public sealed partial class PluginPanelViewModel : ViewModelBase
{
    private readonly IPluginRegistry _registry;

    [ObservableProperty]
    private string _status = "Loading plugins…";

    public PluginPanelViewModel(IPluginRegistry registry)
    {
        _registry = registry;
        _registry.Changed += Refresh;
        Refresh();
    }

    public IReadOnlyList<PluginFeatureItem> Features { get; private set; } = [];

    public void Refresh()
    {
        Features = _registry.Features
            .Select(f => new PluginFeatureItem
            {
                Id = f.Descriptor.Id,
                Name = f.Descriptor.Name,
                Description = f.Descriptor.Description,
                PluginName = f.Descriptor.PluginName,
                IsModule = f.Descriptor.IsModule,
                IsBehavior = f.Descriptor.IsBehavior,
                Enabled = f.Enabled,
                IsActiveModule = f.Descriptor.Id == _registry.ActiveModuleId,
                LoadError = f.LoadError
            })
            .ToList();

        var pluginCount = _registry.Plugins.Count;
        var featureCount = _registry.Features.Count;
        Status = pluginCount == 0
            ? $"No plugins in folder. Built-in loader ready ({featureCount} features)."
            : $"{pluginCount} plugin(s), {featureCount} feature(s). Active: {_registry.ActiveModuleId ?? "none"}";

        OnPropertyChanged(nameof(Features));
    }

    [RelayCommand]
    private void ReloadPlugins()
    {
        _registry.Reload();
        Refresh();
    }

    [RelayCommand]
    private void ToggleFeature(PluginFeatureItem? item)
    {
        if (item is null)
            return;

        _registry.SetEnabled(item.Id, !item.Enabled);
        Refresh();
    }

    [RelayCommand]
    private void SetActiveModule(PluginFeatureItem? item)
    {
        if (item is null || !item.IsModule)
            return;

        _registry.SetActiveModule(item.Id);
        Refresh();
    }
}

public sealed class PluginFeatureItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string PluginName { get; init; }
    public bool IsModule { get; init; }
    public bool IsBehavior { get; init; }
    public bool Enabled { get; init; }
    public bool IsActiveModule { get; init; }
    public string? LoadError { get; init; }
    public string Kind => IsModule ? "Module" : IsBehavior ? "Behavior" : "Feature";
}
