using DarkBot.Net.Application.Managers;
using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Models.Auth;

namespace DarkBot.Net.Application.Services.Auth;

public sealed class LoginAppService(ILoginService loginService, IBackpageApi backpage, StatsManager stats)
    : Contracts.ILoginAppService
{
    public Task<LoginData> LoginWithCredentialsAsync(
        string username,
        string password,
        string? captchaToken,
        CancellationToken cancellationToken = default) =>
        loginService.LoginWithCredentialsAsync(username, password, captchaToken, cancellationToken);

    public Task<LoginData> LoginWithSidAsync(
        string server,
        string sid,
        CancellationToken cancellationToken = default) =>
        loginService.LoginWithSidAsync(server, sid, cancellationToken);

    public void ApplySession(LoginData loginData) =>
        loginService.ApplySession(loginData, backpage, stats);
}
