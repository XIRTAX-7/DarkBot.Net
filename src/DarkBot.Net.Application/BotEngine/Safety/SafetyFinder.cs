using DarkBot.Net.Application.BotEngine.Modules;
using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Entities;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.BotEngine.Safety;

/// <summary>Минимальный SafetyFinder — low HP + NPC в радиусе (полный порт — Phase 6).</summary>
public sealed class SafetyFinder(ModuleContext context)
{
    private const int DangerDistance = 1500;
    private const double LowHpRatio = 0.25;

    public string? Status { get; private set; }

    /// <summary>true = безопасно тикать модуль; false = идёт escape.</summary>
    public bool Tick()
    {
        if (IsHeroLowHp())
        {
            context.Movement.MoveRandom();
            Status = "Escaping: low HP";
            return false;
        }

        var threat = FindNearestNpc();
        if (threat is not null)
        {
            EscapeFrom(threat);
            Status = "Escaping: NPC";
            return false;
        }

        Status = null;
        return true;
    }

    private bool IsHeroLowHp()
    {
        var hero = context.Hero;
        if (!hero.IsValid)
            return false;

        var maxHp = hero.Health.MaxHp;
        if (maxHp <= 0)
            return false;

        return hero.Health.Hp / (double)maxHp < LowHpRatio;
    }

    private INpc? FindNearestNpc()
    {
        var hero = context.Hero;
        INpc? nearest = null;
        var nearestDistance = double.MaxValue;

        foreach (var npc in context.Entities.Npcs)
        {
            if (!npc.IsValid)
                continue;

            var distance = hero.DistanceTo(npc);
            if (distance >= DangerDistance || distance >= nearestDistance)
                continue;

            nearest = npc;
            nearestDistance = distance;
        }

        return nearest;
    }

    private void EscapeFrom(ILocatable threat)
    {
        var hero = context.Hero;
        var angle = threat.AngleTo(hero);
        var distance = DangerDistance + 100;
        var target = LocatablePoint.Of(threat, angle, distance);
        context.Movement.MoveTo(target.X, target.Y);
    }
}
