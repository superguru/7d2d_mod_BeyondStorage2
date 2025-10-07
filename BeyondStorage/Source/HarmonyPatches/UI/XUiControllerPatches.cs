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
            var btnBeyondSmartLootSort = UIControlHelpers.GetSmartLootSortButton(__instance);
            if (btnBeyondSmartLootSort != null)
            {
                btnBeyondSmartLootSort.OnPress -= SmartSortingCommon.SmartLootSort_EventHandler;
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: Smart loot sorting button event handler removed");
#endif
            }
        }
        catch (System.Exception ex)
        {
            ModLogger.Warning($"{d_MethodName}: Error during cleanup: {ex.Message}");
        }
    }
}