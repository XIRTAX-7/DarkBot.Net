using DarkBot.Net.Core.Models.Game;

namespace DarkBot.Net.Application.Contracts;

public interface IGameConnectionStatusAppService
{
    event Action? StatusChanged;

    GameConnectionStatusSnapshot GetStatus();
}
