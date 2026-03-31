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
#if DEBUG
        const string d_MethodName = nameof(XUiC_BackpackWindow_Init_Postfix);
#endif
        var btnBeyondSmartPlayerInventoryPush = UIControlHelpers.GetSmartPlayerInventoryPushButton(__instance);
        if (btnBeyondSmartPlayerInventoryPush != null)
        {
            btnBeyondSmartPlayerInventoryPush.OnPress += SmartSortingCommon.SmartPlayerInventoryPush_EventHandler;
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Smart player inventory push button initialized");
#endif
        }
    }
}