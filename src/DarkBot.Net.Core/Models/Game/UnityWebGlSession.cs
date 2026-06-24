namespace DarkBot.Net.Core.Models.Game;

/// <summary>Учётные данные для WebView autologin через Frida RPC.</summary>
public sealed record UnityWebGlSession(
    string? Username = null,
    string? Password = null);
