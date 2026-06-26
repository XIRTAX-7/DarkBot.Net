using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Managers;

namespace DarkBot.Net.Application.Services.Movement;

public sealed class MovementAppService(IMovementApi movement) : IMovementAppService
{
    public void MoveTo(double x, double y) => movement.MoveTo(x, y);
}
