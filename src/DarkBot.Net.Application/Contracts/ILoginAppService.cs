using DarkBot.Net.Core.Models.Auth;

namespace DarkBot.Net.Application.Contracts;

public interface ILoginAppService
{
    bool HasSavedCredentials { get; }

    bool TryLoadSavedCredentials(out SavedCredentials credentials);

    void LoginWithCredentials(string username, string password, bool rememberMe);
}
