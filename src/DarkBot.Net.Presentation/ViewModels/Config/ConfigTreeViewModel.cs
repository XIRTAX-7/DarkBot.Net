using System.Collections.ObjectModel;
using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Config;
using DarkBot.Net.Presentation.Resources;
using DarkBot.Net.Presentation.ViewModels.Shared;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;

namespace DarkBot.Net.Presentation.ViewModels.Config;

public sealed partial class ConfigTreeViewModel : ViewModelBase
{
    private readonly IConfigAppService? _config;
    private bool _suppressPush;

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

    public ConfigTreeViewModel(IConfigAppService config)
    {
        _config = config;
        SelectedVisibilityLevel = DefaultVisibilityLevel;
        InitializeReactiveState();
        LoadFromService();
        _config.ConfigChanged += OnConfigChanged;
    }

    /// <summary>Design-time / preview без backend.</summary>
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

    public bool IsEditable => _config?.IsEditable ?? true;

    public string ActiveProfileName => _config?.ActiveProfile ?? ConfigProfileNames.DefaultUser;

    public string AiControlBadge =>
        _config?.ActiveOwner is ProfileOwner.Ai
            ? $"AI: {_config.GetAiProfileSummary()?.Name ?? "ai-pve"}"
            : string.Empty;

    public bool ShowAiControlBadge =>
        _config?.ActiveOwner is ProfileOwner.Ai;

    public bool IsSettingVisible(ConfigSettingVisibility required) =>
        required.IsVisibleAt(SelectedVisibilityLevel?.Level ?? ConfigSettingVisibilityDefaults.Level);

    [Reactive] private ConfigSidebarItem? _selectedSidebarItem = SidebarItems[0];
    [Reactive] private ConfigVisibilityLevelItem? _selectedVisibilityLevel = DefaultVisibilityLevel;
    [Reactive] private ConfigProfileSummaryDto? _selectedProfile;

    [Reactive] private bool _stayAwayFromEnemies;
    [Reactive] private bool _autoCloak;
    [Reactive] private string _autoCloakKey = string.Empty;
    [Reactive] private double _collectRadius = 400;
    [Reactive] private bool _ignoreContestedBoxes = true;
    [Reactive] private string _boxSearchFilter = string.Empty;
    [Reactive] private string _newProfileName = string.Empty;

    public ObservableCollection<BoxInfoRowViewModel> Boxes { get; } = [];
    public ObservableCollection<BoxInfoRowViewModel> FilteredBoxes { get; } = [];
    public ObservableCollection<ConfigProfileSummaryDto> UserProfiles { get; } = [];

    [ReactiveCommand]
    private void SwitchProfile()
    {
        if (_config is null || SelectedProfile is null)
            return;

        _config.SwitchProfile(SelectedProfile.Name);
        LoadFromService();
    }

    [ReactiveCommand]
    private void CreateProfile()
    {
        if (_config is null || string.IsNullOrWhiteSpace(NewProfileName))
            return;

        _config.CreateProfile(NewProfileName.Trim());
        NewProfileName = string.Empty;
        LoadFromService();
    }

    [ReactiveCommand]
    private void DeleteProfile()
    {
        if (_config is null || SelectedProfile is null)
            return;

        _config.DeleteProfile(SelectedProfile.Name);
        LoadFromService();
    }

    [ReactiveCommand]
    private void DuplicateProfile()
    {
        if (_config is null || SelectedProfile is null || string.IsNullOrWhiteSpace(NewProfileName))
            return;

        _config.DuplicateProfile(SelectedProfile.Name, NewProfileName.Trim());
        NewProfileName = string.Empty;
        LoadFromService();
    }

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

        this.WhenAnyValue(x => x.StayAwayFromEnemies)
            .Skip(1)
            .Subscribe(value => PushCollectSetting("collect.stay_away_from_enemies", value));

        this.WhenAnyValue(x => x.AutoCloak)
            .Skip(1)
            .Subscribe(value => PushCollectSetting("collect.auto_cloak", value));

        this.WhenAnyValue(x => x.CollectRadius)
            .Skip(1)
            .Subscribe(value => PushCollectSetting("collect.radius", value));

        this.WhenAnyValue(x => x.IgnoreContestedBoxes)
            .Skip(1)
            .Subscribe(value => PushCollectSetting("collect.ignore_contested_boxes", value));

        RefreshFilteredBoxes();
    }

    private void LoadFromService()
    {
        if (_config is null)
            return;

        _suppressPush = true;

        UserProfiles.Clear();
        foreach (var profile in _config.ListUserProfiles())
            UserProfiles.Add(profile);

        SelectedProfile = UserProfiles.FirstOrDefault(p =>
            p.Name.Equals(_config.ActiveProfile, StringComparison.OrdinalIgnoreCase))
            ?? UserProfiles.FirstOrDefault();

        var collect = _config.LoadCollectState();
        StayAwayFromEnemies = collect.StayAwayFromEnemies;
        AutoCloak = collect.AutoCloak;
        CollectRadius = collect.CollectRadius;
        IgnoreContestedBoxes = collect.IgnoreContestedBoxes;

        Boxes.Clear();
        foreach (var box in collect.Boxes)
        {
            var row = new BoxInfoRowViewModel(box.Name, box.Collect, box.WaitTime, box.Priority);
            WireBoxRow(row);
            Boxes.Add(row);
        }

        if (Boxes.Count == 0)
            SeedSampleBoxes();

        RefreshFilteredBoxes();
        _suppressPush = false;

        this.RaisePropertyChanged(nameof(IsEditable));
        this.RaisePropertyChanged(nameof(ActiveProfileName));
        this.RaisePropertyChanged(nameof(AiControlBadge));
        this.RaisePropertyChanged(nameof(ShowAiControlBadge));
    }

    private void WireBoxRow(BoxInfoRowViewModel row)
    {
        row.WhenAnyValue(x => x.Collect)
            .Skip(1)
            .Subscribe(value => PushCollectSetting($"collect.box_infos.{row.Name}.should_collect", value));

        row.WhenAnyValue(x => x.WaitTime)
            .Skip(1)
            .Subscribe(value => PushCollectSetting($"collect.box_infos.{row.Name}.wait_time", value));

        row.WhenAnyValue(x => x.Priority)
            .Skip(1)
            .Subscribe(value => PushCollectSetting($"collect.box_infos.{row.Name}.priority", value));
    }

    private void PushCollectSetting(string path, object value)
    {
        if (_suppressPush || _config is null || !IsEditable)
            return;

        _config.UpdateCollectSetting(path, value);
    }

    private void OnConfigChanged(object? sender, EventArgs e) =>
        LoadFromService();

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
