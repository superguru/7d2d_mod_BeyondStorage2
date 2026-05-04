using System.Collections.Generic;
using System.Threading;
using BeyondStorage.Game.Functions;
using BeyondStorage.Game.Item;
using HarmonyLib;

namespace BeyondStorage.Harmony.Extends;

[HarmonyPatch(typeof(XUiM_PlayerInventory))]
internal static class XUiM_PlayerInventory_Ext
{
#if DEBUG
    private static long s_callCounter = 0;
#endif

    // Cache the reflection calls at class level (best performance)
    private static readonly System.Reflection.MethodInfo s_onBackpackChanged =
        AccessTools.Method(typeof(XUiM_PlayerInventory), "onBackpackItemsChanged");
    private static readonly System.Reflection.MethodInfo s_onToolbeltChanged =
        AccessTools.Method(typeof(XUiM_PlayerInventory), "onToolbeltItemsChanged");

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiM_PlayerInventory.RemoveItems))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool XUiM_PlayerInventory_RemoveItems_Prefix(XUiM_PlayerInventory __instance, IList<ItemStack> _itemStacks, int _multiplier, IList<ItemStack> _removedItems)
    {
        // Use common sequential removal method: Bag → Toolbelt → Storage
        ItemCommon.RemoveItemsSequential(__instance.Backpack, __instance.Toolbelt, _itemStacks, _multiplier, true, _removedItems);

        // Use cached method references (fastest)
        s_onBackpackChanged?.Invoke(__instance, null);
        s_onToolbeltChanged?.Invoke(__instance, null);

        return false; // Skip the original method completely
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiM_PlayerInventory.CanSwapItems), [typeof(ItemStack), typeof(ItemStack), typeof(int)])]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool XUiM_PlayerInventory_CanSwapItems_Prefix(XUiM_PlayerInventory __instance, ItemStack _removedStack, ItemStack _addedStack, int _slotNumber, ref bool __result)
    {
        __result = PurchasingCommon.CanSwapItems(__instance, _removedStack, _addedStack, _slotNumber);
        return false; // Skip original method
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiM_PlayerInventory.CountAvailableSpaceForItem), [typeof(ItemValue), typeof(bool)])]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiM_PlayerInventory_CountAvailableSpaceForItem_Postfix(XUiM_PlayerInventory __instance, ItemValue itemValue, bool limitToOneStack, ref int __result)
    {
        // Use PurchasingCommon calculation
        __result = PurchasingCommon.GetEnhancedAvailableSpace(itemValue, __result, limitToOneStack, nameof(PurchasingCommon.GetEnhancedAvailableSpace));
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiM_PlayerInventory.GetItemCountWithMods))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiM_PlayerInventory_GetItemCountWithMods_Postfix(XUiM_PlayerInventory __instance, ItemValue _itemValue, ref int __result)
    {
#if DEBUG
        //const string d_MethodName = nameof(XUiM_PlayerInventory_GetItemCountWithMods_Postfix);
        var callCount = Interlocked.Increment(ref s_callCounter);
#endif
        var entityPlayerCount = __result;
        var storageCount = ItemCommon.ItemCommon_GetStorageItemCount(_itemValue);
        __result = entityPlayerCount + storageCount;
#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName} [{callCount}]: item: {_itemValue.ItemClass.Name}; result {__result} = entityPlayerCount: {entityPlayerCount} + storageCount: {storageCount}");
#endif
    }
}