namespace DarkBot.Net.Ui.Services;

public sealed record BotUiSnapshot(
    bool HeroValid,
    int HeroId,
    double HeroX,
    double HeroY,
    int HeroHp,
    int HeroMaxHp,
    int MapId,
    string MapName,
    int MapWidth,
    int MapHeight,
    bool BotRunning,
    long TickCount,
    double LastTickMs,
    double Credits,
    double Uridium,
    double Experience,
    double Honor,
    int Ping,
    string BackpageStatus,
    bool BackpageValid);
