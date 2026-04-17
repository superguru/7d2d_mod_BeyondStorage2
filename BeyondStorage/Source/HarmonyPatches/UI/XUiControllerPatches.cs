using BeyondStorage.Source.Game.UI;
using BeyondStorage.Source.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.UI;

/// <summary>
/// Harmony patches for XUiController cleanup to handle smart loot sort button event unsubscription
/// </summary>
[HarmonyPatch(typeof(XUiController))]
#if DEBUG
[HarmonyDebug]
#endif
internal static class XUiControllerPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiController.Cleanup))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiController_Cleanup_Prefix(XUiController __instance)
    {
        const string d_MethodName = nameof(XUiController_Cleanup_Prefix);

        try
        {
            var btnBeyondSmartCollectorPush = UIControlHelpers.GetSmartCollectorPushButton(__instance);
            if (btnBeyondSmartCollectorPush != null)
            {
                btnBeyondSmartCollectorPush.OnPress -= SmartSortingCommon.SmartCollectorPush_EventHandler;
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: Smart collector push button event handler removed");
#endif
            }

            var btnBeyondSmartDronePullLoadout = UIControlHelpers.GetSmartDroneInventoryPullLoadoutButton(__instance);
            if (btnBeyondSmartDronePullLoadout != null)
            {
                btnBeyondSmartDronePullLoadout.OnPress -= SmartSortingCommon.SmartDroneInventoryPullLoadout_EventHandler;
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: Smart drone pull loadout button event handler removed");
#endif
            }

            var btnBeyondSmartLootWindowPush = UIControlHelpers.GetSmartLootWindowPushButton(__instance);
            if (btnBeyondSmartLootWindowPush != null)
            {
                btnBeyondSmartLootWindowPush.OnPress -= SmartSortingCommon.SmartLootWindowPush_EventHandler;
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: Smart loot window push button event handler removed");
#endif
            }

            var btnBeyondSmartPlayerInventoryPullLoadout = UIControlHelpers.GetSmartPlayerInventoryPullLoadoutButton(__instance);
            if (btnBeyondSmartPlayerInventoryPullLoadout != null)
            {
                btnBeyondSmartPlayerInventoryPullLoadout.OnPress -= SmartSortingCommon.SmartPlayerInventoryPullLoadout_EventHandler;
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: Smart player inventory pull loadout button event handler removed");
#endif
            }

            var btnBeyondSmartPlayerInventoryPush = UIControlHelpers.GetSmartPlayerInventoryPushButton(__instance);
            if (btnBeyondSmartPlayerInventoryPush != null)
            {
                btnBeyondSmartPlayerInventoryPush.OnPress -= SmartSortingCommon.SmartPlayerInventoryPush_EventHandler;
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: Smart player inventory push button event handler removed");
#endif
            }

            var btnBeyondSmartVehiclePullLoadout = UIControlHelpers.GetSmartVehiclePullLoadoutButton(__instance);
            if (btnBeyondSmartVehiclePullLoadout != null)
            {
                btnBeyondSmartVehiclePullLoadout.OnPress -= SmartSortingCommon.SmartVehiclePullLoadout_EventHandler;
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: Smart vehicle pull loadout button event handler removed");
#endif
            }

            var btnBeyondSmartVehiclePush = UIControlHelpers.GetSmartVehiclePushButton(__instance);
            if (btnBeyondSmartVehiclePush != null)
            {
                btnBeyondSmartVehiclePush.OnPress -= SmartSortingCommon.SmartVehiclePush_EventHandler;
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: Smart vehicle push button event handler removed");
#endif
            }

            var btnBeyondSmartWorkstationOutputPush = UIControlHelpers.GetSmartWorkstationOutputPushButton(__instance);
            if (btnBeyondSmartWorkstationOutputPush != null)
            {
                btnBeyondSmartWorkstationOutputPush.OnPress -= SmartSortingCommon.SmartWorkstationOutputPush_EventHandler;
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: Smart workstation output push button event handler removed");
#endif
            }
        }
        catch (System.Exception ex)
        {
            ModLogger.Warning($"{d_MethodName}: Error during cleanup: {ex.Message}");
        }
    }
}