namespace DarkBot.Net.Application.BotEngine.Managers;

/// <summary>Static portal on a map — ported from Java StarManager/StarBuilder.</summary>
public readonly record struct MapPortalInfo(int X, int Y, string TargetName, string TargetShortName);
