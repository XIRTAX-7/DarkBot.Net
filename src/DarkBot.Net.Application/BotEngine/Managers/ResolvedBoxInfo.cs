using DarkBot.Net.Core.Config;
using DarkBot.Net.Core.Config.Types;

namespace DarkBot.Net.Application.BotEngine.Managers;

/// <summary>IBoxInfo из collect.box_infos на момент snapshot.</summary>
internal sealed class ResolvedBoxInfo : IBoxInfo
{
    public bool ShouldCollect { get; set; }
    public int WaitTime { get; set; }
    public int Priority { get; set; }

    public static ResolvedBoxInfo FromRecord(BoxInfoRecord record) =>
        new()
        {
            ShouldCollect = record.ShouldCollect,
            WaitTime = record.WaitTime,
            Priority = record.Priority,
        };

    public static ResolvedBoxInfo Disabled { get; } = new()
    {
        ShouldCollect = false,
        Priority = int.MaxValue,
    };
}
