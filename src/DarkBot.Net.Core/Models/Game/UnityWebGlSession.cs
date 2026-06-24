namespace DarkBot.Net.Core.Models.Game;

/// <summary>Учётные данные для WebView autologin (заполнение формы логина).</summary>
public sealed record UnityWebGlSession(
    string InstanceHost,
    string Sid,
    string WebGlJson,
    string? Username = null,
    string? Password = null);
