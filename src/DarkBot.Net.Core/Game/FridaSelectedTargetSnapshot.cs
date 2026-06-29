namespace DarkBot.Net.Core.Game;

/// <summary>Выбранная в игре цель (не герой) из Frida /status.</summary>
public sealed record FridaSelectedTargetSnapshot(
    int UserId,
    int Hp,
    int MaxHp,
    int Shield,
    int MaxShield,
    string? Name,
    bool IsEnemy,
    double X,
    double Y,
    bool IsOnMap = true);
