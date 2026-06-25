using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DarkBot.Net.Core.Config;
using DarkBot.Net.Core.Managers;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace DarkBot.Net.Presentation.ViewModels;

public sealed partial class ConfigTreeNodeViewModel : ViewModelBase
{
    public ConfigTreeNodeViewModel(IConfigSetting<object> setting, string path)
    {
        Setting = setting;
        Path = path;
        Name = setting.Name;
        Key = setting.Key;
        IsParent = setting is IConfigSettingParent parent && parent.Children.Count > 0;

        if (setting is IConfigSettingParent p)
        {
            foreach (var child in p.Children.Values.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
            {
                var childPath = string.IsNullOrEmpty(path) ? child.Key : $"{path}.{child.Key}";
                Children.Add(new ConfigTreeNodeViewModel(child, childPath));
            }
        }

        UpdateEditorValue();

        this.WhenAnyValue(x => x.BoolValue)
            .Subscribe(value =>
            {
                if (!IsBoolean)
                    return;

                Setting.Value = value;
                EditorValue = value.ToString();
            });

        this.WhenAnyValue(x => x.EditorValue)
            .Subscribe(value =>
            {
                if (IsBoolean)
                    return;

                Setting.Value = ConvertValue(value, Setting.ValueType) ?? Setting.Value;
            });
    }

    public IConfigSetting<object> Setting { get; }
    public string Path { get; }
    public string Name { get; }
    public string Key { get; }
    public bool IsParent { get; }
    public IList<ConfigTreeNodeViewModel> Children { get; } = new List<ConfigTreeNodeViewModel>();

    [Reactive] private string _editorValue = string.Empty;
    [Reactive] private bool _isBoolean;
    [Reactive] private bool _boolValue;

    public void UpdateEditorValue()
    {
        if (Setting.ValueType == typeof(bool))
        {
            IsBoolean = true;
            BoolValue = Setting.Value is bool b && b;
            EditorValue = BoolValue.ToString();
            return;
        }

        IsBoolean = false;
        EditorValue = Setting.Value?.ToString() ?? string.Empty;
    }

    private static object? ConvertValue(string text, Type type)
    {
        if (type == typeof(string))
            return text;

        if (type == typeof(int) && int.TryParse(text, out var i))
            return i;

        if (type == typeof(double) && double.TryParse(text, out var d))
            return d;

        return text;
    }
}

public sealed partial class ConfigTreeViewModel : ViewModelBase
{
    public static IReadOnlyList<ConfigSidebarItem> SidebarItems { get; } =
    [
        new("Главная", ConfigSidebarSection.Main),
        new("Сбор", ConfigSidebarSection.Collect),
        new("Убийство NPC", ConfigSidebarSection.NpcKill),
        new("PET", ConfigSidebarSection.Pet),
        new("Группа", ConfigSidebarSection.Group),
        new("Прочее", ConfigSidebarSection.Other),
        new("Настройки бота", ConfigSidebarSection.BotSettings),
        new("Plugins", ConfigSidebarSection.Plugins),
    ];

    public ConfigTreeViewModel(IConfigApi config)
    {
        Profile = config.CurrentProfile;
        RootNodes = config.ConfigRoot is IConfigSettingParent parent
            ? parent.Children.Values
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .Select(c => new ConfigTreeNodeViewModel(c, c.Key))
                .ToList()
            : [];

        SeedSampleBoxes();
        InitializeReactiveState();
    }

    /// <summary>Конструктор для design mode / XAML previewer.</summary>
    public ConfigTreeViewModel()
    {
        Profile = "default";
        RootNodes = [];
        SeedSampleBoxes();
        InitializeReactiveState();
    }

    public string Profile { get; }
    public IReadOnlyList<ConfigTreeNodeViewModel> RootNodes { get; }

    /// <summary>Пункты бокового меню (экземпляр для биндинга).</summary>
    public IReadOnlyList<ConfigSidebarItem> SidebarMenuItems => SidebarItems;

    public ConfigSidebarSection SelectedSection =>
        (SelectedSidebarItem ?? SidebarItems[0]).Section;

    public string PlaceholderMessage =>
        SelectedSidebarItem is null
            ? "Раздел в разработке."
            : $"Раздел «{SelectedSidebarItem.Title}» в разработке.";

    [Reactive] private ConfigSidebarItem? _selectedSidebarItem = SidebarItems[0];
    [Reactive] private ConfigTreeNodeViewModel? _selectedNode;

    [Reactive] private bool _stayAwayFromEnemies;
    [Reactive] private bool _autoCloak;
    [Reactive] private string _autoCloakKey = string.Empty;
    [Reactive] private double _collectRadius = 400;
    [Reactive] private bool _ignoreContestedBoxes = true;
    [Reactive] private string _boxSearchFilter = string.Empty;

    public ObservableCollection<BoxInfoRowViewModel> Boxes { get; } = [];
    public ObservableCollection<BoxInfoRowViewModel> FilteredBoxes { get; } = [];

    private void InitializeReactiveState()
    {
        this.WhenAnyValue(x => x.SelectedSidebarItem)
            .Subscribe(item =>
            {
                if (item is null)
                    SelectedSidebarItem = SidebarItems[0];

                this.RaisePropertyChanged(nameof(SelectedSection));
                this.RaisePropertyChanged(nameof(PlaceholderMessage));
            });

        this.WhenAnyValue(x => x.BoxSearchFilter)
            .Subscribe(_ => RefreshFilteredBoxes());

        RefreshFilteredBoxes();
    }

    private void SeedSampleBoxes()
    {
        if (Boxes.Count > 0)
            return;

        foreach (var box in CreateSampleBoxes())
            Boxes.Add(box);
    }

    private static IEnumerable<BoxInfoRowViewModel> CreateSampleBoxes() =>
    [
        new("BONUS_BOX", true, 0, 1),
        new("PROMETID", true, 0, 2),
        new("ENDURIUM", true, 0, 3),
        new("TERBIUM", true, 0, 4),
        new("PALLADIUM", false, 0, 5),
        new("XAMOR", false, 0, 6),
        new("BOLTRUM", false, 0, 7),
        new("SCRAPIUM", false, 0, 8),
    ];

    private void RefreshFilteredBoxes()
    {
        FilteredBoxes.Clear();

        var filter = BoxSearchFilter.Trim();
        foreach (var box in Boxes)
        {
            if (filter.Length > 0
                && !box.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            FilteredBoxes.Add(box);
        }
    }
}
