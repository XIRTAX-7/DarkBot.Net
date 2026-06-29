using System.Text.Json.Serialization;
using DarkBot.Net.Core.Config.Types;

namespace DarkBot.Net.Core.Config;

public sealed record BotProfileMeta(
    string DisplayName,
    ProfileOwner Owner,
    int SchemaVersion = 1);

public sealed record BotProfileGeneral(
    string CurrentModule,
    int WorkingMap,
    int SafetyWait);

public sealed record BoxInfoRecord(
    bool ShouldCollect,
    int Priority,
    int WaitTime) : IBoxInfo
{
    [JsonIgnore]
    public string Name => string.Empty;

    bool IBoxInfo.ShouldCollect
    {
        get => ShouldCollect;
        set => throw new NotSupportedException("BoxInfoRecord is immutable; update via BotProfileDocument.");
    }

    int IBoxInfo.WaitTime
    {
        get => WaitTime;
        set => throw new NotSupportedException("BoxInfoRecord is immutable; update via BotProfileDocument.");
    }

    int IBoxInfo.Priority
    {
        get => Priority;
        set => throw new NotSupportedException("BoxInfoRecord is immutable; update via BotProfileDocument.");
    }
}

public sealed record BotProfileCollect(
    int Radius,
    bool StayAwayFromEnemies,
    bool AutoCloak,
    bool IgnoreContestedBoxes,
    Dictionary<string, BoxInfoRecord> BoxInfos);

public sealed record BotProfileDocument(
    BotProfileMeta Meta,
    BotProfileGeneral General,
    BotProfileCollect Collect);
