using System.Linq;
using BeyondStorage.Data;
using BeyondStorage.Game.UI;
using BeyondStorage.Infrastructure;
using BeyondStorage.UI;
using HarmonyLib;

namespace BeyondStorage.Harmony.Extends;

[HarmonyPatch(typeof(XUiC_LootWindow))]
internal static class XUiC_LootWindow_Ext
{
    // Store the previous LockedSlots state for comparison
    private static PackedBoolArray s_previousLockedSlots = null;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_LootWindow.Init))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_LootWindow_Init_Postfix(XUiC_LootWindow __instance)
    {
#if DEBUG
        //const string d_MethodName = nameof(XUiC_LootWindow_Init_Postfix);
#endif
        var btnBeyondSmartDronePullLoadout = UIControlHelpers.GetSmartDroneInventoryPullLoadoutButton(__instance);
        if (btnBeyondSmartDronePullLoadout != null)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Smart drone pull loadout button initialized");
#endif
            btnBeyondSmartDronePullLoadout.OnPress += SmartSortingCommon.SmartDroneInventoryPullLoadout_EventHandler;
        }

        var btnBeyondSmartLootWindowPush = UIControlHelpers.GetSmartLootWindowPushButton(__instance);
        if (btnBeyondSmartLootWindowPush != null)
        {
            btnBeyondSmartLootWindowPush.OnPress += SmartSortingCommon.SmartLootWindowPush_EventHandler;
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: Smart loot window push button initialized");
#endif
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_LootWindow.UpdateLockedSlots))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_LootWindow_UpdateLockedSlots_Prefix(XUiC_LootWindow __instance, XUiC_ContainerStandardControls _csc)
    {
#if DEBUG
        const string d_MethodName = nameof(XUiC_LootWindow_UpdateLockedSlots_Prefix);
#endif
        if (_csc == null)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: _csc parameter is null");
#endif
            s_previousLockedSlots = null;
            return;
        }

        // Save the current LockedSlots state before the update
        s_previousLockedSlots = _csc.LockedSlots;

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Saved LockedSlots state: {(s_previousLockedSlots != null ? $"Count={s_previousLockedSlots.Length}" : "null")}");
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_LootWindow.UpdateLockedSlots))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_LootWindow_UpdateLockedSlots_Postfix(XUiC_LootWindow __instance, XUiC_ContainerStandardControls _csc)
    {
        if (_csc != null)
        {
            var currentLockedSlots = _csc.LockedSlots;
            if (currentLockedSlots == null)
            {
                return;
            }

            ItemStack itemStack = null;

            var slots = __instance?.lootContainer?.GetSlots();
            if (slots != null)
            {
                // Check if any of the slots contain currency items
                bool containsCurrency = slots.Any(slot => CurrencyCache.IsCurrencyItem(slot));
                if (containsCurrency)
                {
                    // Trigger a currency refresh after slot lock changes when currency is present
                    itemStack = CurrencyCache.GetEmptyCurrencyStack();
                }
            }

            UIRefreshHelper.LogAndRefreshUI(StackOps.Stack_LockStateChange_Operation, itemStack: itemStack, callCount: 0);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_LootWindow.OnOpen))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_LootWindow_OnOpen_Postfix(XUiC_LootWindow __instance)
    {
        const string d_MethodName = nameof(XUiC_LootWindow_OnOpen_Postfix);

        // Check for duplicate window open (should not happen)
        if (WindowStateManager.IsPlayerStorageOpen())
        {
            ModLogger.DebugLog($"{d_MethodName}: LootWindow is already open for storage. This should not happen!");
        }

        var tileEntity = __instance?.te;
        if (tileEntity == null)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: TileEntity is null, cannot determine if this is a storage container.");
#endif
            return;
        }

        bool isPlayerStorage = false;

        // Check for TEFeatureStorage using comprehensive feature detection
        if (tileEntity.TryGetSelfOrFeature(out TEFeatureStorage storage) && storage != null)
        {
            isPlayerStorage = true;
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: LootWindow opened for storage: {storage}");
#endif
        }

        EntityDrone drone = null;
        if (!isPlayerStorage)
        {
            if (WindowStateManager.IsDroneWindow(tileEntity, out drone, out string matchedTypeName, out string reason))
            {
                isPlayerStorage = true;
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: LootWindow opened for Drone. Reason: {reason}");
#endif
            }
        }

        // Check for player owned/created storage, for example player crafted desk safes, refrigerators, lockers, etc.
        if (!isPlayerStorage)
        {
            isPlayerStorage = tileEntity.bPlayerStorage;
        }

        WindowStateManager.OnStorageWindowOpened(__instance, isPlayerStorage, drone);

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: LootWindow opened isPlayerStorage: {isPlayerStorage}, te: {tileEntity}, bPlayerStorage: {tileEntity.bPlayerStorage}, lootListName: {tileEntity.lootListName}");
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_LootWindow.OnClose))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_LootWindow_OnClose_Postfix(XUiC_LootWindow __instance)
    {
        WindowStateManager.OnStorageWindowClosed(__instance);

        // Clear the saved locked slots state when the window closes
        s_previousLockedSlots = null;

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: LootWindow closed");
#endif
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_LootWindow.GetBindingValueInternal))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool XUiC_LootWindow_GetBindingValueInternal_Prefix(XUiC_LootWindow __instance, ref string _value, string _bindingName, ref bool __result)
    {
        switch (_bindingName)
        {
            case "bs_is_drone_window_open":
                _value = WindowStateManager.IsDroneWindowOpen() ? "true" : "false";
                __result = true;
                return false; // Skip original method

            case "bs_is_player_storage_open":
                _value = WindowStateManager.IsPlayerStorageOpen() ? "true" : "false";
                __result = true;
                return false; // Skip original method
        }

        return true; // Run original method for other bindings
    }
}
