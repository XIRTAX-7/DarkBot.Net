using DarkBot.Net.Presentation.Models.Main.Map;

namespace DarkBot.Net.Presentation.Models.Main;

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
