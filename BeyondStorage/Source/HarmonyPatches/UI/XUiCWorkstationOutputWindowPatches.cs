using BeyondStorage.Source.Game.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.UI;

[HarmonyPatch(typeof(XUiC_WorkstationOutputWindow))]
#if DEBUG
[HarmonyDebug]
#endif
internal static class XUiCWorkstationOutputWindowPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_WorkstationOutputWindow.Init))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_WorkstationOutputWindow_Init_Postfix(XUiC_WorkstationOutputWindow __instance)
    {
#if DEBUG
        //const string d_MethodName = nameof(XUiC_WorkstationOutputWindow_Init_Postfix);
#endif
        var btnBeyondSmartButton = UIControlHelpers.GetSmartWorkstationOutputPushButton(__instance);
        if (btnBeyondSmartButton != null)
        {
            btnBeyondSmartButton.OnPress += SmartSortingCommon.SmartWorkstationOutputPush_EventHandler;
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Smart workstation output push button initialized");
#endif
        }
    }
}