using DarkBot.Net.Api.Game.Entities;

namespace DarkBot.Net.Api.Managers;

/// <summary>Port of eu.darkbot.api.managers.OreAPI (subset for Phase 4).</summary>
public interface IOreApi : IApi.ISingleton
{
    int GetAmount(Ore ore);
    void SellOre(Ore ore);
    bool CanSellOres { get; }
    bool ShowTrade(bool show, IStation.IRefinery? tradePoint = null);

    enum Ore
    {
        Prometium = 0,
        Endurium = 1,
        Terbium = 2,
        Xenomit = 3,
        Prometid = 4,
        Duranium = 5,
        Promerium = 6,
        Seprom = 7,
        Palladium = 8,
        Osmium = 28
    }
}
