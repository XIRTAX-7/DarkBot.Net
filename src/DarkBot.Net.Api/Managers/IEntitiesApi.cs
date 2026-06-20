using DarkBot.Net.Api.Events;
using DarkBot.Net.Api.Game.Entities;

namespace DarkBot.Net.Api.Managers;

public interface IEntitiesApi : IApi.ISingleton
{
    IReadOnlyCollection<INpc> Npcs { get; }
    IReadOnlyCollection<IPet> Pets { get; }
    IReadOnlyCollection<IPlayer> Players { get; }
    IReadOnlyCollection<IShip> Ships { get; }
    IReadOnlyCollection<IBox> Boxes { get; }
    IReadOnlyCollection<IMine> Mines { get; }
    IReadOnlyCollection<IPortal> Portals { get; }
    IReadOnlyCollection<IEntity> All { get; }

    abstract class EntityEvent(IEntity entity) : IEvent
    {
        public IEntity Entity { get; } = entity;
    }

    sealed class EntityCreateEvent(IEntity entity) : EntityEvent(entity);
    sealed class EntityRemoveEvent(IEntity entity) : EntityEvent(entity);
}

public interface IMine : IEntity;
