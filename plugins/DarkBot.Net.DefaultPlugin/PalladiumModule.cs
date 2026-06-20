using DarkBot.Net.Api.Game;
using DarkBot.Net.Api.Game.Entities;
using DarkBot.Net.Api.Managers;
using DarkBot.Net.Plugins.Abstractions;

namespace DarkBot.Net.DefaultPlugin;

[Feature("Palladium Module", "Loot & collect, but when cargo is full travels to 5-2 to sell")]
public sealed class PalladiumModule : IModule, IInstructionProvider
{
    private readonly IBotApi _bot;
    private readonly IStatsApi _stats;
    private readonly IOreApi _ores;
    private readonly IHeroApi _hero;
    private readonly IMovementApi _movement;
    private readonly IGameMap _sellMap;
    private readonly IReadOnlyCollection<IStation> _bases;

    private long _sellClick;

    public PalladiumModule(
        IPluginApi api,
        IBotApi bot,
        IStatsApi stats,
        IOreApi ores,
        IHeroApi hero,
        IMovementApi movement,
        IEntitiesApi entities,
        IStarSystemApi starSystem)
    {
        _bot = bot;
        _stats = stats;
        _ores = ores;
        _hero = hero;
        _movement = movement;
        _sellMap = starSystem.GetByName("5-2");
        _bases = entities.All.OfType<IStation>().ToList();
    }

    public string Instructions() =>
        """
        Recommended settings:
        General -> Working map to 5-3
        Collect -> Set ore_8 wait to 750-800ms (depends on ping)
        Npc killer -> Pirate NPCs -> Kill & Enable Passive (in the Extra Column)
        Avoid zones -> Set in all areas of 5-3 except palladium field & paths to portals
        Preferred zones -> Set Preferred zone in palladium field
        General -> Roaming & Preferred area -> enable only kill npcs in preferred area
        Safety places -> Set Portals to jump: Never.
        Npc killer -> Battleray -> Set low priority (100) so Interceptors are shot first
        """;

    public string? Status =>
        _stats.Cargo >= _stats.MaxCargo && _stats.MaxCargo != 0
            ? "Selling palladium at 5-2"
            : "Farming (loot/collect stub — Phase 4)";

    public void OnTickModule()
    {
        if (_stats.Cargo >= _stats.MaxCargo && _stats.MaxCargo != 0)
            Sell();
        else if (Environment.TickCount64 - 500 > _sellClick && _ores.ShowTrade(false))
        {
            // Full LootCollectorModule arrives later; Phase 4 keeps the sell branch working.
        }
    }

    private void Sell()
    {
        if (_hero.Map.Id != _sellMap.Id)
        {
            // MapModule travel stub — move toward sell map id when maps diverge.
            _movement.MoveRandom();
            return;
        }

        var baseRefinery = _bases
            .OfType<IStation.IRefinery>()
            .FirstOrDefault(b => b.LocationInfo.IsInitialized);

        if (baseRefinery is null)
            return;

        if (_movement.GetClosestDistance(baseRefinery) > 200)
        {
            var angle = baseRefinery.AngleTo(_hero) + Random.Shared.NextDouble() * 0.2 - 0.1;
            _movement.MoveTo(GameLocation.Of(baseRefinery, angle, 100 + 100 * Random.Shared.NextDouble()));
        }
        else if (!_hero.IsMoving() && _ores.ShowTrade(true, baseRefinery)
                 && Environment.TickCount64 - 60_000 > _sellClick)
        {
            _ores.SellOre(IOreApi.Ore.Palladium);
            _sellClick = Environment.TickCount64;
        }
    }
}
