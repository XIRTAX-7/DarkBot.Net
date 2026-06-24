using System.Runtime.Versioning;
using DarkBot.Net.Core.Interfaces.Auth;
using DarkBot.Net.Core.Models.Auth;
using Meziantou.Framework.Win32;
using Microsoft.Extensions.Logging;

namespace DarkBot.Net.Infrastructure.Auth;

/// <summary>Хранение login/password в Windows Credential Manager.</summary>
[SupportedOSPlatform("windows5.1.2600")]
public sealed class WindowsCredentialStore(ILogger<WindowsCredentialStore> logger) : ICredentialStore
{
    private const string CredentialName = "DarkBot.Net.Login";

    public bool HasSaved => TryLoad(out _);

    public bool TryLoad(out SavedCredentials credentials)
    {
        credentials = null!;

        try
        {
            var credential = CredentialManager.ReadCredential(CredentialName);
            if (credential is null
                || string.IsNullOrWhiteSpace(credential.UserName)
                || string.IsNullOrEmpty(credential.Password))
            {
                return false;
            }

            credentials = new SavedCredentials(credential.UserName, credential.Password);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read credentials from Windows Credential Manager");
            return false;
        }
    }

    public void Save(SavedCredentials credentials)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentials.Username);
        ArgumentException.ThrowIfNullOrEmpty(credentials.Password);

        CredentialManager.WriteCredential(
            applicationName: CredentialName,
            userName: credentials.Username,
            secret: credentials.Password,
            persistence: CredentialPersistence.LocalMachine);

        logger.LogInformation(
            "Credentials saved to Windows Credential Manager for user {Username}",
            credentials.Username);
    }

    public void Clear()
    {
        try
        {
            CredentialManager.DeleteCredential(CredentialName);
            logger.LogInformation("Credentials removed from Windows Credential Manager");
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to delete credentials from Windows Credential Manager");
        }
    }
}
