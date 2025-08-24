using BeyondStorage.Scripts.Game.Item;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Block;

[HarmonyPatch(typeof(ItemActionRepair))]
internal static class ItemActionRepairPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ItemActionRepair.canRemoveRequiredItem), [typeof(ItemInventoryData), typeof(ItemStack)])]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void ItemActionRepair_canRemoveRequiredItem_Postfix(ItemActionRepair __instance, ItemInventoryData _data, ItemStack _itemStack, ref bool __result)
    {
        // If player already has enough items, no need to check storage
        if (__result)
        {
            return;
        }

        // Check if storage has the required repair items
        __result = ItemCommon.HasItemInStorage(_itemStack.itemValue);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ItemActionRepair.removeRequiredItem), [typeof(ItemInventoryData), typeof(ItemStack)])]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool ItemActionRepair_removeRequiredItem_Prefix(ItemActionRepair __instance, ItemInventoryData _data, ItemStack _itemStack, ref bool __result)
    {
        // Get player entity from the inventory data
        EntityPlayer entityPlayer = _data.holdingEntity as EntityPlayer;
        if (entityPlayer == null)
        {
            __result = false;
            return false; // Skip original method
        }

        // Use sequential removal: Bag → Toolbelt → Storage (enhanced logic)
        // Original game uses Toolbelt → Bag, but we use Bag → Toolbelt for consistency
        int totalRemoved = ItemCommon.RemoveItemsSequential(
            entityPlayer.bag,
            entityPlayer.inventory,
            _itemStack.itemValue,
            _itemStack.count
        );

        // Return true if we removed the exact amount needed (original logic)
        __result = totalRemoved == _itemStack.count;

        return false; // Skip original method
    }
}