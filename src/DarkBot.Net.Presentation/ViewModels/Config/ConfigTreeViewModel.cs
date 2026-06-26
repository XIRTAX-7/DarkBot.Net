using System.Collections.ObjectModel;
using DarkBot.Net.Core.Config;
using DarkBot.Net.Presentation.Resources;
using DarkBot.Net.Presentation.ViewModels.Shared;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace DarkBot.Net.Presentation.ViewModels.Config;

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

    public static ConfigVisibilityLevelItem DefaultVisibilityLevel { get; } =
        new(ConfigSettingVisibilityDefaults.Level, UiStrings.Config_VisibilityLevel_Basic);

    public static IReadOnlyList<ConfigVisibilityLevelItem> VisibilityLevels { get; } =
    [
        DefaultVisibilityLevel,
        new(ConfigSettingVisibility.Intermediate, UiStrings.Config_VisibilityLevel_Intermediate),
        new(ConfigSettingVisibility.Advanced, UiStrings.Config_VisibilityLevel_Advanced),
        new(ConfigSettingVisibility.Developer, UiStrings.Config_VisibilityLevel_Developer),
    ];

    public ConfigTreeViewModel()
    {
        SeedSampleBoxes();
        SelectedVisibilityLevel = DefaultVisibilityLevel;
        InitializeReactiveState();
    }

    public ConfigSidebarSection SelectedSection =>
        (SelectedSidebarItem ?? SidebarItems[0]).Section;

    public string PlaceholderMessage =>
        SelectedSidebarItem is null
            ? "Раздел в разработке."
            : $"Раздел «{SelectedSidebarItem.Title}» в разработке.";

    public bool ShowIntermediateSettings =>
        IsSettingVisible(ConfigSettingVisibility.Intermediate);

    public bool ShowAdvancedSettings =>
        IsSettingVisible(ConfigSettingVisibility.Advanced);

    public bool IsSettingVisible(ConfigSettingVisibility required) =>
        required.IsVisibleAt(SelectedVisibilityLevel?.Level ?? ConfigSettingVisibilityDefaults.Level);

    [Reactive] private ConfigSidebarItem? _selectedSidebarItem = SidebarItems[0];
    [Reactive] private ConfigVisibilityLevelItem? _selectedVisibilityLevel = DefaultVisibilityLevel;

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

        this.WhenAnyValue(x => x.SelectedVisibilityLevel)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(ShowIntermediateSettings));
                this.RaisePropertyChanged(nameof(ShowAdvancedSettings));
            });

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
