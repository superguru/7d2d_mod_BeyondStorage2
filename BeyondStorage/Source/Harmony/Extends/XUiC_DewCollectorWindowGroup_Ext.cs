using BeyondStorage.Game.UI;
using BeyondStorage.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.Harmony.Extends;

[HarmonyPatch(typeof(XUiC_DewCollectorWindowGroup))]
internal static class XUiC_DewCollectorWindowGroup_Ext
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_DewCollectorWindowGroup.OnOpen))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_CollectorWindowGroup_OnOpen_Postfix(XUiC_DewCollectorWindowGroup __instance)
    {
        const string d_MethodName = nameof(XUiC_CollectorWindowGroup_OnOpen_Postfix);

        // Check for duplicate window open (should not happen)
        if (WindowStateManager.IsCollectorWindowOpen())
        {
            ModLogger.Error($"{d_MethodName}: Collector Window is already open. This should not happen!");
        }

        WindowStateManager.OnCollectorWindowOpened(__instance);

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Collector Window Opened");
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_DewCollectorWindowGroup.OnClose))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_CollectorWindowGroup_OnClose_Postfix(XUiC_DewCollectorWindowGroup __instance)
    {
#if DEBUG
        //const string d_MethodName = nameof(XUiC_CollectorWindowGroup_OnClose_Postfix);
#endif

        WindowStateManager.OnCollectorWindowClosed(__instance);

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Collector Window Closed");
#endif
    }
}