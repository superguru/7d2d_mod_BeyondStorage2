using BeyondStorage.Scripts.ContainerLogic;
using BeyondStorage.Scripts.Utils;
using static ModEvents;

namespace BeyondStorage.Scripts.Common;

public static class EventsUtil
{
    public static void GameStartDone(ref SGameStartDoneData data)
    {
        Logger.DebugLog("Game Start: Initializing...");
        TileEntityLockManager.Init();
    }

    public static void GameShutdown(ref SGameShutdownData data)
    {
        Logger.DebugLog("Game Shutdown: Cleaning up...");
        TileEntityLockManager.Cleanup();
    }

    // public static void PlayerDisconnected(ClientInfo client, bool arg2) {
    //     Logger.DebugLog($"Player Disconnected: {client}; somebool {arg2}");
    // }
}