using BeyondStorage.Scripts.Multiplayer;
using static ModEvents;

namespace BeyondStorage.Scripts.Infrastructure;

public static class ModLifecycleManager
{
    public static void GameStartDone(ref SGameStartDoneData data)
    {
        ModLogger.DebugLog("Game Start: Initializing...");
        TileEntityLockManager.Init();
    }

    public static void GameShutdown(ref SGameShutdownData data)
    {
        ModLogger.DebugLog("Game Shutdown: Cleaning up...");
        TileEntityLockManager.Cleanup();
    }

    // public static void PlayerDisconnected(ClientInfo _clientInfo, bool _gameShuttingDown) {
    //     ModLogger.DebugLog($"Player Disconnected: clientInfo {_clientInfo}; gameShuttingDown {_gameShuttingDown}");
    // }
}