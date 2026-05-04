using System;
using System.Collections.Generic;
using BeyondStorage.Game.UI;
using BeyondStorage.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.Harmony.Extends;

/// <summary>
/// Harmony patches for XUiController cleanup to handle smart loot sort button event unsubscription
/// </summary>
[HarmonyPatch(typeof(XUiController))]
#if DEBUG
[HarmonyDebug]
#endif
internal static class XUiController_Ext
{
    private static readonly List<(Func<XUiController, XUiController> getter, Action<XUiController> unsubscribe)> s_buttonHandlerPairs =
    [
        (UIControlHelpers.GetSmartCollectorPushButton,             btn => btn.OnPress -= SmartSortingCommon.SmartCollectorPush_EventHandler),
        (UIControlHelpers.GetSmartDroneInventoryPullLoadoutButton, btn => btn.OnPress -= SmartSortingCommon.SmartDroneInventoryPullLoadout_EventHandler),
        (UIControlHelpers.GetSmartLootWindowPushButton,            btn => btn.OnPress -= SmartSortingCommon.SmartLootWindowPush_EventHandler),
        (UIControlHelpers.GetSmartPlayerInventoryPullLoadoutButton,btn => btn.OnPress -= SmartSortingCommon.SmartPlayerInventoryPullLoadout_EventHandler),
        (UIControlHelpers.GetSmartPlayerLootingPullLoadoutButton,  btn => btn.OnPress -= SmartSortingCommon.SmartPlayerInventoryPullLoadout_EventHandler),
        (UIControlHelpers.GetSmartPlayerInventoryPushButton,       btn => btn.OnPress -= SmartSortingCommon.SmartPlayerInventoryPush_EventHandler),
        (UIControlHelpers.GetSmartPlayerLootingPushButton,         btn => btn.OnPress -= SmartSortingCommon.SmartPlayerInventoryPush_EventHandler),
        (UIControlHelpers.GetSmartVehiclePullLoadoutButton,        btn => btn.OnPress -= SmartSortingCommon.SmartVehiclePullLoadout_EventHandler),
        (UIControlHelpers.GetSmartVehiclePushButton,               btn => btn.OnPress -= SmartSortingCommon.SmartVehiclePush_EventHandler),
        (UIControlHelpers.GetSmartWorkstationOutputPushButton,     btn => btn.OnPress -= SmartSortingCommon.SmartWorkstationOutputPush_EventHandler),
    ];

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
            foreach (var (getter, unsubscribe) in s_buttonHandlerPairs)
            {
                var btn = getter(__instance);
                if (btn != null)
                {
                    unsubscribe(btn);
                }
            }
        }
        catch (Exception ex)
        {
            ModLogger.Warning($"{d_MethodName}: Error during cleanup: {ex.Message}");
        }
    }
}