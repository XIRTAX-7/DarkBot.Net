using DarkBot.Net.Application.BotEngine.Managers;
using DarkBot.Net.Application.BotEngine.Modules;
using DarkBot.Net.Core.Config;

namespace DarkBot.Net.Application.Contracts;

/// <summary>Фасад конфигурации для Presentation — только user-профили editable.</summary>
public interface IConfigAppService
{
    ProfileOwner ActiveOwner { get; }
    string ActiveProfile { get; }
    bool IsEditable { get; }

    IReadOnlyList<ConfigProfileSummaryDto> ListUserProfiles();
    ConfigProfileSummaryDto? GetActiveProfile();
    AiProfileSummaryDto? GetAiProfileSummary();

    void SwitchProfile(string profileName);
    void CreateProfile(string profileName, string? displayName = null);
    void RenameProfile(string profileName, string displayName);
    void DeleteProfile(string profileName);
    void DuplicateProfile(string sourceProfileName, string newProfileName);

    ConfigCollectStateDto LoadCollectState();
    void UpdateCollectSetting(string path, object value);

    ConfigMainStateDto LoadMainState();
    void UpdateGeneralSetting(string path, object value);

    Task SaveAsync(CancellationToken cancellationToken = default);

    event EventHandler? ConfigChanged;
}

public sealed record ConfigProfileSummaryDto(string Name, string DisplayName);

public sealed record AiProfileSummaryDto(string Name, string DisplayName, int WorkingMap, string CurrentModule);

public sealed record ConfigCollectStateDto(
    bool StayAwayFromEnemies,
    bool AutoCloak,
    double CollectRadius,
    bool IgnoreContestedBoxes,
    IReadOnlyList<ConfigBoxInfoRowDto> Boxes);

public sealed record ConfigBoxInfoRowDto(
    string Name,
    bool Collect,
    int WaitTime,
    int Priority);

public sealed record ConfigMainStateDto(
    string CurrentModule,
    int WorkingMap,
    int SafetyWait,
    IReadOnlyList<ModuleOption> Modules,
    IReadOnlyList<MapOptionDto> WorkingMaps);

public sealed record MapOptionDto(int MapId, string DisplayName);
