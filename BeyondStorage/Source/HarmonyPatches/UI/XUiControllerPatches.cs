using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Source.Game.UI;
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
            var btnBeyondSmartPlayerInventoryPush = UIControlHelpers.GetSmartPlayerInventoryPushButton(__instance);
            if (btnBeyondSmartPlayerInventoryPush != null)
            {
                btnBeyondSmartPlayerInventoryPush.OnPress -= SmartSortingCommon.SmartPlayerInventoryPush_EventHandler;
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: Smart player inventory push button event handler removed");
#endif
            }

            var btnBeyondSmartCollectorPush = UIControlHelpers.GetSmartCollectorPushButton(__instance);
            if (btnBeyondSmartCollectorPush != null)
            {
                btnBeyondSmartCollectorPush.OnPress -= SmartSortingCommon.SmartCollectorPush_EventHandler;
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: Smart collector push button event handler removed");
#endif
            }

            var btnBeyondSmartVehiclePush = UIControlHelpers.GetSmartVehiclePushButton(__instance);
            if (btnBeyondSmartVehiclePush != null)
            {
                btnBeyondSmartVehiclePush.OnPress -= SmartSortingCommon.SmartVehiclePush_EventHandler;
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: Smart vehicle push button event handler removed");
#endif
            }

            var btnBeyondSmartWorkstationOutputPush = UIControlHelpers.GetSmartWorkstationOutputPushButton(__instance);
            if (btnBeyondSmartWorkstationOutputPush != null)
            {
                btnBeyondSmartWorkstationOutputPush.OnPress -= SmartSortingCommon.SmartWorkstationOutputPush_EventHandler;
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: Smart workstation output push button event handler removed");
#endif
            }
        }
        catch (System.Exception ex)
        {
            ModLogger.Warning($"{d_MethodName}: Error during cleanup: {ex.Message}");
        }
    }
}