using DarkBot.Net.Application.Contracts;
using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Interfaces.Game;

namespace DarkBot.Net.Application.Services.Shell;

public sealed class AppShellAppService(
    ICredentialStore credentialStore,
    IGameSessionStore sessionStore) : IAppShellAppService
{
    public bool ShouldOpenMainScreen =>
        sessionStore.HasSession || credentialStore.HasSaved;
}
