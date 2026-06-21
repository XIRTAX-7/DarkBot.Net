using DarkBot.Net.Core.Game.Entities;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.Managers;

public sealed class OreApi : IOreApi
{
    public bool TradeOpen { get; private set; }
    public IStation.IRefinery? ActiveRefinery { get; private set; }
    public IOreApi.Ore? LastSold { get; private set; }

    public int GetAmount(IOreApi.Ore ore) => 0;

    public void SellOre(IOreApi.Ore ore) => LastSold = ore;

    public bool CanSellOres => TradeOpen;

    public bool ShowTrade(bool show, IStation.IRefinery? tradePoint = null)
    {
        if (show)
        {
            if (tradePoint is null)
                return false;

            TradeOpen = true;
            ActiveRefinery = tradePoint;
            return true;
        }

        if (!TradeOpen)
            return false;

        TradeOpen = false;
        ActiveRefinery = null;
        return true;
    }
}
