namespace DarkBot.Net.Agent.Windows.Memory;

public static class GameMemoryUtil
{
    public static bool IsValidPtr(long ptr) => ptr > GameMemoryConstants.BadPtr;
}
