using BeyondStorage.Game.UI;
using BeyondStorage.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.Harmony.Extends;

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
            //ModLogger.DebugLog($"{d_MethodName}: Smart player inventory only pull loadout button initialized");
#endif
        }

        var btnBeyondSmartPushButton = UIControlHelpers.GetSmartPlayerInventoryPushButton(__instance);
        if (btnBeyondSmartPushButton != null)
        {
            btnBeyondSmartPushButton.OnPress += SmartSortingCommon.SmartPlayerInventoryPush_EventHandler;
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Smart player inventory only push button initialized");
#endif
        }

        var btnBeyondSmartLootingPullButton = UIControlHelpers.GetSmartPlayerLootingPullLoadoutButton(__instance);
        if (btnBeyondSmartLootingPullButton != null)
        {
            btnBeyondSmartLootingPullButton.OnPress += SmartSortingCommon.SmartPlayerInventoryPullLoadout_EventHandler;
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Smart player looting pull loadout button initialized");
#endif
        }

        var btnBeyondSmartPushLootingButton = UIControlHelpers.GetSmartPlayerLootingPushButton(__instance);
        if (btnBeyondSmartPushLootingButton != null)
        {
            btnBeyondSmartPushLootingButton.OnPress += SmartSortingCommon.SmartPlayerInventoryPush_EventHandler;
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Smart player looting push button initialized");
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
        //ModLogger.DebugLog($"{d_MethodName}: Backpack window opened");
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
        //const string d_MethodName = nameof(XUiC_BackpackWindow_OnClose_Postfix);
#endif
        WindowStateManager.OnBackpackWindowClosed(__instance);

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Backpack window closed");
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_BackpackWindow.GetBindingValueInternal))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_BackpackWindow_GetBindingValueInternal_Postfix(XUiC_BackpackWindow __instance, ref string value, string bindingName, ref bool __result)
    {
#if DEBUG
        //const string d_MethodName = nameof(XUiC_BackpackWindow_GetBindingValueInternal_Postfix);
#endif

        if (__result)
        {
            if (bindingName == "lootingorvehiclestorage")
            {
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: bindingName={bindingName}, value={value}");
#endif
            }
            return;  // Binding already handled by original method, no need to process further
        }

        switch (bindingName)
        {
            case "bs_is_player_backpack_only":
                value = WindowStateManager.IsOnlyPlayerBackpackOpen();
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: bindingName={bindingName}, value={value}");
#endif
                __result = true;

                break;
        }
    }
}