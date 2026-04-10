using BeyondStorage.Source.Infrastructure;
using BeyondStorage.Source.Game.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.UI;

[HarmonyPatch(typeof(XUiC_DewCollectorWindow))]
#if DEBUG
[HarmonyDebug]
#endif
internal static class XUiCDewCollectorWindowPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_DewCollectorWindow.Init))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_DewCollectorWindow_Init_Postfix(XUiC_DewCollectorWindow __instance)
    {
#if DEBUG
        const string d_MethodName = nameof(XUiC_DewCollectorWindow_Init_Postfix);
#endif
        var btnBeyondSmartButton = UIControlHelpers.GetSmartCollectorPushButton(__instance);
        if (btnBeyondSmartButton != null)
        {
            btnBeyondSmartButton.OnPress += SmartSortingCommon.SmartCollectorPush_EventHandler;
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Smart collector push button initialized");
#endif
        }
    }
}