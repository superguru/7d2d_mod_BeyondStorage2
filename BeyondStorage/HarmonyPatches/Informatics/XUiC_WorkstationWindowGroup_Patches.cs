using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_WorkstationWindowGroup))]
internal static class XUiC_WorkstationWindowGroup_Patches
{
    private static XUiC_WorkstationWindowGroup s_windowInstance = null;
    private static bool s_isWorkstationWindowOpen = false;
    private static readonly object s_lockObject = new();

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_WorkstationWindowGroup.OnOpen))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_WorkstationWindowGroup_OnOpen_Postfix(XUiC_WorkstationWindowGroup __instance)
    {
        const string d_MethodName = nameof(XUiC_WorkstationWindowGroup_OnOpen_Postfix);

        lock (s_lockObject)
        {
            if (s_isWorkstationWindowOpen || (s_windowInstance != null))
            {
                ModLogger.Error($"{d_MethodName}: Workstation Window is already open. This should not happen!");

                s_isWorkstationWindowOpen = false; // Reset the flag to prevent confusion
                s_windowInstance = null;
            }

            s_windowInstance = __instance;
            s_isWorkstationWindowOpen = true;

#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Workstation Window Opened");
#endif
        }
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
        lock (s_lockObject)
        {
            s_windowInstance = null;
            s_isWorkstationWindowOpen = false;

#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Workstation Window Closed");
#endif
        }
    }

    public static bool IsWorkstationWindowOpen()
    {
        lock (s_lockObject)
        {
            return s_isWorkstationWindowOpen;
        }
    }

    public static bool IsCurrentlyActiveWindow(XUiC_WorkstationWindowGroup window)
    {
        lock (s_lockObject)
        {
            return s_isWorkstationWindowOpen && (s_windowInstance == window);
        }
    }

    public static XUiC_WorkstationWindowGroup GetCurrentlyActiveWorkstation()
    {
        lock (s_lockObject)
        {
            return s_isWorkstationWindowOpen ? s_windowInstance : null;
        }
    }
}