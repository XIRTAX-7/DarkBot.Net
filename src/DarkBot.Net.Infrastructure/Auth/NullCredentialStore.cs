using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Models.Auth;

namespace DarkBot.Net.Infrastructure.Auth;

/// <summary>Заглушка для платформ без Windows Credential Manager.</summary>
public sealed class NullCredentialStore : ICredentialStore
{
    public bool HasSaved => false;

    public bool TryLoad(out SavedCredentials credentials)
    {
        credentials = null!;
        return false;
    }

    public void Save(SavedCredentials credentials) { }

    public void Clear() { }
}
