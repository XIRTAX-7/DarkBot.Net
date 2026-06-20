using DarkBot.Net.Api.Config.Types;
using DarkBot.Net.Api.Game;
using DarkBot.Net.Api.Game.Entities;
using DarkBot.Net.Api.Game.Items;

namespace DarkBot.Net.Api.Managers;

public interface IHeroApi : IPlayer, IApi.ISingleton
{
    IGameMap Map { get; }
    ILockable? LocalTarget { get; }
    T? GetLocalTargetAs<T>() where T : class, ILockable => LocalTarget as T;
    void SetLocalTarget(ILockable? target);
    HeroConfiguration ActiveConfiguration { get; }
    bool IsInMode(IShipMode mode);
    bool SetMode(IShipMode mode);
    bool SetAttackMode(INpc? target);
    bool SetRoamMode();
    bool SetRunMode();
    bool TriggerLaserAttack();
    bool LaunchRocket();
    ISelectableItem.ILaser Laser { get; }
    ISelectableItem.IRocket Rocket { get; }
}
