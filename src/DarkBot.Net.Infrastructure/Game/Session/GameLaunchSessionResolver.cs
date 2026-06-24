using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Models.Auth;
using DarkBot.Net.Core.Models.Game;

namespace DarkBot.Net.Infrastructure.Game.Session;

/// <summary>Восстанавливает параметры запуска из store или сохранённых credentials.</summary>
public sealed class GameLaunchSessionResolver(
    GameSessionStore sessionStore,
    ICredentialStore credentialStore)
{
    public bool HasLaunchSession =>
        sessionStore.HasSession || credentialStore.HasSaved;

    public Task<GameLaunchParameters?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (sessionStore.Current is not null)
            return Task.FromResult<GameLaunchParameters?>(sessionStore.Current);

        if (!credentialStore.TryLoad(out var credentials))
            return Task.FromResult<GameLaunchParameters?>(null);

        return Task.FromResult<GameLaunchParameters?>(ToLaunchParameters(credentials));
    }

    private static GameLaunchParameters ToLaunchParameters(SavedCredentials credentials) =>
        GameLaunchParameters.FromCredentials(credentials.Username, credentials.Password);
}
