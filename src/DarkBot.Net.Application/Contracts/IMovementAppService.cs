namespace DarkBot.Net.Application.Contracts;

public interface IMovementAppService
{
    void MoveTo(double x, double y);

    Task MoveToAsync(double x, double y, CancellationToken cancellationToken = default);
}
