using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_VehicleStorageWindowGroup))]
public class XUiC_VehicleStorageWindowGroup_Patches
{
    private static XUiC_VehicleStorageWindowGroup s_windowInstance = null;
    private static bool s_isVehicleStorageWindowOpen = false;
    private static readonly object s_lockObject = new();

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_VehicleStorageWindowGroup.OnOpen))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void XUiC_VehicleStorageWindowGroup_OnOpen_Postfix(XUiC_VehicleStorageWindowGroup __instance)
    {
        const string d_MethodName = nameof(XUiC_VehicleStorageWindowGroup_OnOpen_Postfix);

        lock (s_lockObject)
        {
            if (s_isVehicleStorageWindowOpen || (s_windowInstance != null))
            {
                ModLogger.Error($"{d_MethodName}: Vehicle Storage Window is already open. This should not happen!");

                s_isVehicleStorageWindowOpen = false; // Reset the flag to prevent confusion
                s_windowInstance = null;
            }

            s_windowInstance = __instance;
            s_isVehicleStorageWindowOpen = true;

#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Vehicle Storage Window Opened");
#endif
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_VehicleStorageWindowGroup.OnClose))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void XUiC_VehicleStorageWindowGroup_OnClose_Postfix(XUiC_VehicleStorageWindowGroup __instance)
    {
#if DEBUG
        const string d_MethodName = nameof(XUiC_VehicleStorageWindowGroup_OnClose_Postfix);
#endif
        lock (s_lockObject)
        {
            s_windowInstance = null;
            s_isVehicleStorageWindowOpen = false;

#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Vehicle Storage Window Closed");
#endif
        }
    }

    public static bool IsVehicleStorageWindowOpen()
    {
        lock (s_lockObject)
        {
            return s_isVehicleStorageWindowOpen;
        }
    }
}
