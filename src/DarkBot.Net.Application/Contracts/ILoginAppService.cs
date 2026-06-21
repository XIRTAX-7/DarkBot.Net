using DarkBot.Net.Core.Models.Auth;

namespace DarkBot.Net.Application.Contracts;

public interface ILoginAppService
{
    Task<LoginData> LoginWithCredentialsAsync(
        string username,
        string password,
        string? captchaToken,
        CancellationToken cancellationToken = default);

    Task<LoginData> LoginWithSidAsync(
        string server,
        string sid,
        CancellationToken cancellationToken = default);

    void ApplySession(LoginData loginData);
}
