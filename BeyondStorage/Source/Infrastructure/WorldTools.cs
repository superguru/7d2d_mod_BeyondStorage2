namespace BeyondStorage.Scripts.Infrastructure;
public static class WorldTools
{
    public static bool IsServer()
    {
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

    /// <summary>
    /// Gets comprehensive diagnostic information about the current world and connection state.
    /// This includes connection type, world existence, game state, and drone manager status.
    /// </summary>
    /// <returns>Formatted string containing all diagnostic information</returns>
    public static string GetWorldDiagnosticState()
    {
        var connectionState = GetConnectionStateInfo();
        var worldState = GetWorldStateInfo();
        var droneState = GetDroneManagerStateInfo();

        return $"Connection: [{connectionState}], World: [{worldState}], Drones: [{droneState}]";
    }

    /// <summary>
    /// Gets connection state information for diagnostic logging.
    /// </summary>
    /// <returns>String containing connection state details</returns>
    private static string GetConnectionStateInfo()
    {
        var isServer = IsServer();
        var isClient = IsClient();
        var isSinglePlayer = IsSinglePlayer();

        return $"IsServer: {isServer}, IsClient: {isClient}, IsSinglePlayer: {isSinglePlayer}";
    }

    /// <summary>
    /// Gets world state information for diagnostic logging.
    /// </summary>
    /// <returns>String containing world state details</returns>
    private static string GetWorldStateInfo()
    {
        var worldExists = IsWorldExists();
        var gameManager = GameManager.Instance;
        var world = gameManager?.World;
        var gameStarted = gameManager?.gameStateManager?.IsGameStarted() ?? false;

        return $"WorldExists: {worldExists}, GameManagerExists: {gameManager != null}, WorldInstance: {world != null}, GameStarted: {gameStarted}";
    }

    /// <summary>
    /// Gets drone manager state information for diagnostic logging.
    /// </summary>
    /// <returns>String containing drone manager state details</returns>
    private static string GetDroneManagerStateInfo()
    {
        var droneManagerExists = DroneManager.Instance != null;
        var serverDroneCount = DroneManager.GetServerDroneCount();
        var dronesActiveExists = droneManagerExists ? DroneManager.Instance.dronesActive != null : false;

        return $"ManagerExists: {droneManagerExists}, ServerDroneCount: {serverDroneCount}, ActiveListExists: {dronesActiveExists}";
    }
}
