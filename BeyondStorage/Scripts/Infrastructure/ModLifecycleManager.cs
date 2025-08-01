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

    // public static void PlayerDisconnected(ClientInfo client, bool arg2) {
    //     ModLogger.DebugLog($"Player Disconnected: {client}; somebool {arg2}");
    // }
}