using BeyondStorage.Scripts.Game.Item;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Block;

[HarmonyPatch(typeof(ItemActionRepair))]
internal static class ItemActionRepairUpgradePatches
{
    // Used For:
    //          Block Upgrade (Check for enough items) Post Patch
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ItemActionRepair.CanRemoveRequiredResource), [typeof(ItemInventoryData), typeof(BlockValue)])]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void ItemActionRepair_CanRemoveRequiredResource_Postfix(ItemActionRepair __instance, ItemInventoryData data, BlockValue blockValue, ref bool __result)
    {
        // If player already has enough items, no need to check storage
        if (__result)
        {
            return;
        }

        // Get the upgrade item for this block
        string upgradeItemName = __instance.GetUpgradeItemName(blockValue.Block);
        if (string.IsNullOrEmpty(upgradeItemName))
        {
            return;
        }

        ItemValue upgradeItemValue = ItemClass.GetItem(upgradeItemName);

        // Check if storage has the required upgrade items
        __result = ItemCommon.HasItemInStorage(upgradeItemValue);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ItemActionRepair.RemoveRequiredResource), [typeof(ItemInventoryData), typeof(BlockValue)])]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool ItemActionRepair_RemoveRequiredResource_Prefix(ItemActionRepair __instance, ItemInventoryData data, BlockValue blockValue, ref bool __result)
    {
        // Replicate original validation logic
        if (!__instance.CanRemoveRequiredResource(data, blockValue))
        {
            __result = false;
            return false; // Skip original method
        }

        global::Block block = blockValue.Block;
        ItemValue itemValue = ItemClass.GetItem(__instance.GetUpgradeItemName(block));

        // Get required count from block properties
        if (!int.TryParse(block.Properties.Values[global::Block.PropUpgradeBlockClassItemCount], out var requiredCount))
        {
            __result = false;
            return false; // Skip original method
        }

        // Get player entity from the inventory data
        EntityPlayer entityPlayer = data.holdingEntity as EntityPlayer;
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
            itemValue,
            requiredCount
        );

        // Success if we removed the exact amount needed (original logic)
        bool success = totalRemoved == requiredCount;

        if (success)
        {
            // Update UI to show item consumption (original logic)
            EntityPlayerLocal entityPlayerLocal = data.holdingEntity as EntityPlayerLocal;
            if (entityPlayerLocal != null && requiredCount != 0)
            {
                entityPlayerLocal.AddUIHarvestingItem(new ItemStack(itemValue, -requiredCount));
            }
        }

        __result = success;
        return false; // Skip original method
    }
}