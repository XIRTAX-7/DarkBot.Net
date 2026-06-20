using DarkBot.Net.Api.Managers;

namespace DarkBot.Net.Core.Managers;

public sealed class RepairApi : IRepairApi
{
    public bool IsDestroyed { get; private set; }
    public string? LastDestroyerName { get; private set; }

    public void SetDestroyed(string? destroyerName)
    {
        IsDestroyed = true;
        LastDestroyerName = destroyerName;
    }

    public void ClearDestroyed() => IsDestroyed = false;
}
