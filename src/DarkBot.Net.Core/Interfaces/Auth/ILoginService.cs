using DarkBot.Net.Core.Managers;
using DarkBot.Net.Core.Models.Auth;

namespace DarkBot.Net.Core.Interfaces.Auth;

public interface ILoginService
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

    void ApplySession(LoginData loginData, IBackpageApi backpage, ISessionMetadataProvider session);
}
