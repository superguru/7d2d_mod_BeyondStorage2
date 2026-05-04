using BeyondStorage.Game.UI;
using HarmonyLib;

namespace BeyondStorage.Harmony.Extends;

[HarmonyPatch(typeof(XUiC_DewCollectorWindow))]
#if DEBUG
[HarmonyDebug]
#endif
internal static class XUiC_DewCollectorWindow_Ext
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_DewCollectorWindow.Init))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_DewCollectorWindow_Init_Postfix(XUiC_DewCollectorWindow __instance)
    {
#if DEBUG
        //const string d_MethodName = nameof(XUiC_DewCollectorWindow_Init_Postfix);
#endif
        var btnBeyondSmartButton = UIControlHelpers.GetSmartCollectorPushButton(__instance);
        if (btnBeyondSmartButton != null)
        {
            btnBeyondSmartButton.OnPress += SmartSortingCommon.SmartCollectorPush_EventHandler;
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Smart collector push button initialized");
#endif
        }
    }
}