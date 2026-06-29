using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.BotEngine.Modules;

/// <summary>Зависимости активного модуля — только managers, не Frida/Infrastructure.</summary>
public sealed class ModuleContext(
    IHeroApi hero,
    IMovementApi movement,
    IEntitiesApi entities,
    IStatsApi stats,
    IConfigApi config,
    IBotMapApi map)
{
    public IHeroApi Hero { get; } = hero;
    public IMovementApi Movement { get; } = movement;
    public IEntitiesApi Entities { get; } = entities;
    public IStatsApi Stats { get; } = stats;
    public IConfigApi Config { get; } = config;
    public IBotMapApi Map { get; } = map;
}
