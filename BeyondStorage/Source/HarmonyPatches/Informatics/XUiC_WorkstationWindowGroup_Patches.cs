using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Source.Game.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_WorkstationWindowGroup))]
internal static class XUiC_WorkstationWindowGroup_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_WorkstationWindowGroup.OnOpen))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_WorkstationWindowGroup_OnOpen_Postfix(XUiC_WorkstationWindowGroup __instance)
    {
        const string d_MethodName = nameof(XUiC_WorkstationWindowGroup_OnOpen_Postfix);

        // Check for duplicate window open (should not happen)
        if (WindowStateManager.IsWorkstationWindowOpen())
        {
            ModLogger.Error($"{d_MethodName}: Workstation Window is already open. This should not happen!");
        }

        WindowStateManager.OnWorkstationWindowOpened(__instance);

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Workstation Window Opened");
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_WorkstationWindowGroup.OnClose))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_WorkstationWindowGroup_OnClose_Postfix(XUiC_WorkstationWindowGroup __instance)
    {
#if DEBUG
#endif

        WindowStateManager.OnWorkstationWindowClosed(__instance);

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Workstation Window Closed");
#endif
    }
}