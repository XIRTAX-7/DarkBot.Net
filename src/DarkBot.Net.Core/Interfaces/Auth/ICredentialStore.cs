using DarkBot.Net.Core.Models.Auth;

namespace DarkBot.Net.Core.Interfaces.Auth;

/// <summary>Локальное хранение учётных данных («Запомнить меня»).</summary>
public interface ICredentialStore
{
    bool HasSaved { get; }

    bool TryLoad(out SavedCredentials credentials);

    void Save(SavedCredentials credentials);

    void Clear();
}
