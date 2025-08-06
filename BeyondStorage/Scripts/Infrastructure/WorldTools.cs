namespace BeyondStorage.Scripts.Infrastructure;
public static class WorldTools
{
    public static bool IsServer()
    {
        return GameManager.IsDedicatedServer || (ConnectionManager.Instance?.IsServer ?? false);
    }
}
