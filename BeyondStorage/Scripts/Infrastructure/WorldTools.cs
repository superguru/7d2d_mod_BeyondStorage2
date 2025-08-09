namespace BeyondStorage.Scripts.Infrastructure;
public static class WorldTools
{
    public static bool IsServer()
    {
        //return GameManager.IsDedicatedServer || (ConnectionManager.Instance?.IsServer ?? false);
        // if (GameManager.Instance == null || !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)

        return SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
    }

    public static bool IsClient()
    {
        return SingletonMonoBehaviour<ConnectionManager>.Instance.IsClient;
    }

    public static bool IsSinglePlayer()
    {
        return SingletonMonoBehaviour<ConnectionManager>.Instance.IsSinglePlayer;
    }

    public static bool IsWorldExists()
    {
        return GameManager.Instance?.World != null;
    }
}
