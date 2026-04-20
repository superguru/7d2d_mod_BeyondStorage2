using BeyondStorage.Source.Game.UI;
using BeyondStorage.Source.Infrastructure;
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
        //const string d_MethodName = nameof(XUiC_BackpackWindow_Init_Postfix);
#endif
        var btnBeyondSmartPullButton = UIControlHelpers.GetSmartPlayerInventoryPullLoadoutButton(__instance);
        if (btnBeyondSmartPullButton != null)
        {
            btnBeyondSmartPullButton.OnPress += SmartSortingCommon.SmartPlayerInventoryPullLoadout_EventHandler;
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Smart player inventory pull loadout button initialized");
#endif
        }

        var btnBeyondSmartPushButton = UIControlHelpers.GetSmartPlayerInventoryPushButton(__instance);
        if (btnBeyondSmartPushButton != null)
        {
            btnBeyondSmartPushButton.OnPress += SmartSortingCommon.SmartPlayerInventoryPush_EventHandler;
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Smart player inventory push button initialized");
#endif
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_BackpackWindow.OnOpen))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_BackpackWindow_OnOpen_Postfix(XUiC_BackpackWindow __instance)
    {
        const string d_MethodName = nameof(XUiC_BackpackWindow_OnOpen_Postfix);

        // Check for duplicate window open (should not happen)
        if (WindowStateManager.IsBackpackWindowOpen())
        {
            ModLogger.DebugLog($"{d_MethodName}: Backpack window is already open for storage. This should not happen!");
        }

        WindowStateManager.OnBackpackWindowOpened(__instance);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Backpack window opened");
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_BackpackWindow.OnClose))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_BackpackWindow_OnClose_Postfix(XUiC_BackpackWindow __instance)
    {
#if DEBUG
        const string d_MethodName = nameof(XUiC_BackpackWindow_OnClose_Postfix);
#endif
        WindowStateManager.OnBackpackWindowClosed(__instance);

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Backpack window closed");
#endif
    }
}