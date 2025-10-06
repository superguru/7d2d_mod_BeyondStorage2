using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Source.Game.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_VehicleStorageWindowGroup))]
internal static class XUiC_VehicleStorageWindowGroup_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_VehicleStorageWindowGroup.OnOpen))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_VehicleStorageWindowGroup_OnOpen_Postfix(XUiC_VehicleStorageWindowGroup __instance)
    {
        const string d_MethodName = nameof(XUiC_VehicleStorageWindowGroup_OnOpen_Postfix);

        // Check for duplicate window open (should not happen)
        if (WindowStateManager.IsVehicleStorageWindowOpen())
        {
            ModLogger.Error($"{d_MethodName}: Vehicle Storage Window is already open. This should not happen!");
        }

        WindowStateManager.OnVehicleStorageWindowOpened(__instance);

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Vehicle Storage Window Opened");
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_VehicleStorageWindowGroup.OnClose))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_VehicleStorageWindowGroup_OnClose_Postfix(XUiC_VehicleStorageWindowGroup __instance)
    {
#if DEBUG
        const string d_MethodName = nameof(XUiC_VehicleStorageWindowGroup_OnClose_Postfix);
#endif

        WindowStateManager.OnVehicleStorageWindowClosed(__instance);

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Vehicle Storage Window Closed");
#endif
    }
}
