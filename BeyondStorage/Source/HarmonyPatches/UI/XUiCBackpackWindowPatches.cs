using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Source.Game.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.UI;

[HarmonyPatch(typeof(XUiC_BackpackWindow))]
#if DEBUG
[HarmonyDebug]
#endif
internal static class XUiCBackpackWindowPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_BackpackWindow.Init))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_BackpackWindow_Init_Postfix(XUiC_BackpackWindow __instance)
    {
        const string d_MethodName = nameof(XUiC_BackpackWindow_Init_Postfix);

        var btnBeyondSmartLootSort = UIControlHelpers.GetSmartLootSortButton(__instance);
        if (btnBeyondSmartLootSort != null)
        {
            btnBeyondSmartLootSort.OnPress += SmartSortingCommon.SmartLootSort_EventHandler;
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Smart loot sorting button initialized");
#endif
        }
    }
}