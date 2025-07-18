using BeyondStorage.Scripts.ContainerLogic;
using BeyondStorage.Scripts.Utils;
using static ModEvents;

namespace BeyondStorage.Scripts.Common;

public static class EventsUtil
{
    public static void GameStartDone(ref SGameStartDoneData data)
    {
        LogUtil.DebugLog("Game Start: Initializing...");
        ContainerUtils.Init();
    }

    public static void GameShutdown(ref SGameShutdownData data)
    {
        LogUtil.DebugLog("Game Shutdown: Cleaning up...");
        ContainerUtils.Cleanup();
    }

    // public static void PlayerDisconnected(ClientInfo client, bool arg2) {
    //     LogUtil.DebugLog($"Player Disconnected: {client}; somebool {arg2}");
    // }
}