namespace DarkBot.Net.Core.Options;

public sealed class GameApiOptions
{
    public const string SectionName = "DarkBot";

    public int FridaBridgeStaleSec { get; set; } = 30;

    public int ClientConnectTimeoutSec { get; set; } = 180;

    public int FridaReadyTimeoutSec { get; set; } = 180;

    /// <summary>Минимальный интервал между автоматическими перезапусками клиента (сек).</summary>
    public int ClientAutoRestartCooldownSec { get; set; } = 30;

    /// <summary>После скольки секунд «зависшего» connect/reconnect выполнять полный перезапуск клиента.</summary>
    public int ClientStuckConnectRestartSec { get; set; } = 180;

    public int ConnectPollIntervalMs { get; set; } = 400;

    public int FridaReadyPollIntervalMs { get; set; } = 500;

    /// <summary>Имя процесса Unity-клиента (без .exe).</summary>
    public string UnityProcessName { get; set; } = "DarkOrbit";

    /// <summary>Путь к Unity Frida bridge agent. Пусто — авто-поиск TS bundle с legacy fallback.</summary>
    public string UnityBridgeAgentPath { get; set; } = string.Empty;

    /// <summary>Каталог установки Unity IL2CPP x86-клиента (GameAssembly.dll).</summary>
    public string UnityGameInstallPath { get; set; } = @"C:\DarkOrbit_Version1.1.102";

    /// <summary>Имя exe в каталоге установки.</summary>
    public string UnityGameExecutableName { get; set; } = "DarkOrbit.exe";

    /// <summary>Авторизация через Frida: WebView form fill + естественный UpdateWebData.</summary>
    public bool UnityAuthViaHook { get; set; } = true;

    /// <summary>Задержка перед ранним Frida attach для bootstrap-хуков (сек).</summary>
    public int UnityEarlyAttachDelaySec { get; set; } = 3;

    /// <summary>Задержка перед Frida attach без hook-auth (сек) — снижает GC crash на экране логина.</summary>
    public int UnityFridaAttachDelaySec { get; set; } = 20;

    /// <summary>Таймаут ожидания Addressable-обновления клиента (сек).</summary>
    public int UnityClientUpdateTimeoutSec { get; set; } = 300;

    /// <summary>Таймаут ожидания inject сессии после обновления (сек).</summary>
    public int UnitySessionInjectTimeoutSec { get; set; } = 120;
}
