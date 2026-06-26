namespace DarkBot.Net.Application.Contracts;

/// <summary>Решения навигации shell-окна (login vs main).</summary>
public interface IAppShellAppService
{
    bool ShouldOpenMainScreen { get; }
}
