using DarkBot.Net.Core.Game;
using DarkBot.Net.Core.Game.Entities;

namespace DarkBot.Net.Infrastructure.Game;

/// <summary>Port of com.github.manolo8.darkbot.core.api.GameAPI — native bridge surface (no JNI).</summary>
public interface IGameApi
{
    interface IBase
    {
        int Version { get; }
        void Tick() { }
    }

    interface IWindow : IBase
    {
        void CreateWindow();
        void SetData(string url, string sid, string preloader, string vars);
        IReadOnlyList<IProcessInfo> Processes { get; }
        void OpenProcess(long pid);

        interface IProcessInfo
        {
            int Pid { get; }
            string Name { get; }
        }
    }

    interface IHandler : IBase
    {
        bool IsValid { get; }
        long MemoryUsage { get; }
        double CpuUsage => 0;
        void Reload();
        void SetSize(int width, int height);
        void SetVisible(bool visible);
        void SetMinimized(bool minimized);
    }

    interface IMemory : IBase
    {
        int ReadInt(long address);
        long ReadLong(long address);
        double ReadDouble(long address);
        bool ReadBoolean(long address);
        byte[] ReadBytes(long address, int length);
        void ReadBytes(long address, byte[] buffer, int length);
        void WriteInt(long address, int value);
        void WriteLong(long address, long value);
        void WriteDouble(long address, double value);
        void WriteBoolean(long address, bool value);
        void WriteBytes(long address, params byte[] bytes);
    }

    interface IInteraction : IBase
    {
        void KeyClick(int keyCode);
        void SendText(string text);
        void MouseMove(int x, int y);
        void MouseDown(int x, int y);
        void MouseUp(int x, int y);
        void MouseClick(int x, int y);
    }

    interface IDirectInteraction : IBase
    {
        void SetMaxFps(int maxFps);
        void LockEntity(int id);
        void SelectEntity(IEntity entity);
        void MoveShip(ILocatable destination);
        void CollectBox(IBox box);
    }
}
