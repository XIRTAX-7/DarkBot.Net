using DarkBot.Net.Core.Interfaces.Game;
using DarkBot.Net.Core.Models.Game;
using DarkBot.Net.Infrastructure.Game.Bridge;

namespace DarkBot.Net.Application.Tests;

public sealed class UnityBridgeStateProbeTests
{
    [Fact]
    public void Refresh_TargetWithoutMatchingEntity_StillExposesFocusWithIsOnMapFalse()
    {
        var source = new FakeBridgeStatusSource
        {
            Status = new FridaBridgeStatus
            {
                Ready = true,
                HeroId = 42,
                HeroMaxHp = 200_000,
                TargetUserId = 999,
                TargetHp = 5_000,
                TargetMaxHp = 10_000,
                TargetShield = 800,
                TargetMaxShield = 800,
                TargetShipType = "71",
                Entities = [],
            },
        };
        var probe = new UnityBridgeStateProbe(source);

        probe.Refresh();

        Assert.NotNull(probe.SelectedTarget);
        Assert.Equal(999, probe.SelectedTarget!.UserId);
        Assert.False(probe.SelectedTarget.IsOnMap);
        Assert.Equal(0, probe.SelectedTarget.X);
        Assert.Empty(probe.Entities);
    }

    [Fact]
    public void Refresh_TargetWithMatchingEntity_MarksOnMapAndCopiesPosition()
    {
        var source = new FakeBridgeStatusSource
        {
            Status = new FridaBridgeStatus
            {
                Ready = true,
                HeroId = 42,
                HeroMaxHp = 200_000,
                TargetUserId = 999,
                TargetHp = 5_000,
                TargetMaxHp = 10_000,
                TargetShield = 800,
                TargetMaxShield = 800,
                Entities =
                [
                    new FridaBridgeEntity
                    {
                        Id = 999,
                        X = 1100,
                        Y = 1200,
                        Kind = "npc",
                        Label = "Streuner",
                        IsEnemy = true,
                    },
                ],
            },
        };
        var probe = new UnityBridgeStateProbe(source);

        probe.Refresh();

        Assert.NotNull(probe.SelectedTarget);
        Assert.True(probe.SelectedTarget!.IsOnMap);
        Assert.Equal(1100, probe.SelectedTarget.X);
        Assert.Equal(1200, probe.SelectedTarget.Y);
        Assert.Single(probe.Entities);
    }

    private sealed class FakeBridgeStatusSource : IGameBridgeStatusSource
    {
        public FridaBridgeStatus? Status { get; set; }

        public FridaBridgeStatus? CurrentStatus => Status;

        public UnityBridgeAgentStatus? AgentStatus => null;

        public UnityBridgeRuntimePhase RuntimePhase => UnityBridgeRuntimePhase.OnMap;

        public event Action? StatusChanged;

        public bool RefreshStatus() => Status is not null;
    }
}
