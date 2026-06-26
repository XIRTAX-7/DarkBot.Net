using DarkBot.Net.Presentation.Services.Main.Map;

namespace DarkBot.Net.Presentation.Services.Main;

public sealed record BotUiSnapshot(
    bool BotRunning,
    long TickCount,
    double LastTickMs,
    double Credits,
    double Uridium,
    double Experience,
    double Honor,
    int Ping,
    MapRenderSnapshot Map);
