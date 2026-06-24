namespace DarkBot.Net.Core.Options;

public sealed class GameApiOptions
{
    public const string SectionName = "DarkBot";

    public GameApiMode BrowserApi { get; set; } = GameApiMode.UnityClient;

    public string LibPath { get; set; } = "./lib";

    public string ClassesPath { get; set; } = string.Empty;

    public string DarkBotJarPath { get; set; } = string.Empty;

    public string JvmWorkingDirectory { get; set; } = string.Empty;

    public int Width { get; set; } = 1280;

    public int Height { get; set; } = 720;

    public bool Use3D { get; set; }

    public bool UseProxy { get; set; }

    public bool ForceGameLanguage { get; set; }

    public string? GameLanguage { get; set; }

    public int FridaApiPort { get; set; } = 44570;

    public int FridaBridgeHeartbeatSec { get; set; } = 15;

    public int FridaBridgeStaleSec { get; set; } = 30;

    public int ControlPort { get; set; } = 44568;

    public string DarkorbitClientPath { get; set; } = string.Empty;

    public int ClientConnectTimeoutSec { get; set; } = 180;

    public int FridaReadyTimeoutSec { get; set; } = 180;

    /// <summary>Минимальный интервал между автоматическими перезапусками клиента (сек).</summary>
    public int ClientAutoRestartCooldownSec { get; set; } = 30;

    /// <summary>После скольки секунд «зависшего» connect/reconnect выполнять полный перезапуск клиента.</summary>
    public int ClientStuckConnectRestartSec { get; set; } = 180;

    public int ConnectPollIntervalMs { get; set; } = 400;

    public int FridaReadyPollIntervalMs { get; set; } = 500;

    public int MovementTimeoutMs { get; set; } = 8000;

    /// <summary>Имя процесса Unity-клиента (без .exe).</summary>
    public string UnityProcessName { get; set; } = "DarkOrbit";

    /// <summary>Путь к unity_bridge_agent.js. Пусто — авто-поиск в DarkOrbit_Version1.1.102.</summary>
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
