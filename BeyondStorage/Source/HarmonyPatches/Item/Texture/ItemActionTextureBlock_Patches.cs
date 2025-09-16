using BeyondStorage.Scripts.Game.Item;
using HarmonyLib;
using static ItemActionTextureBlock;

namespace BeyondStorage.HarmonyPatches.Item;

[HarmonyPatch(typeof(ItemActionTextureBlock))]
internal static class ItemActionTextureBlockPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ItemActionTextureBlock.checkAmmo))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool ItemActionTextureBlock_checkAmmo_Prefix(ItemActionTextureBlock __instance, ItemActionData _actionData, ref bool __result)
    {
        // Handle infinite ammo and creative modes first (same as original)
        if (__instance.InfiniteAmmo || GameStats.GetInt(EnumGameStats.GameModeId) == 2 || GameStats.GetInt(EnumGameStats.GameModeId) == 8)
        {
            __result = true;
            return false; // Skip original method
        }

        // Get entity-held ammo count (equivalent to original logic)
        EntityAlive holdingEntity = _actionData.invData.holdingEntity;
        int bagAmmoCount = holdingEntity.bag.GetItemCount(__instance.currentMagazineItem);
        int inventoryAmmoCount = holdingEntity.inventory.GetItemCount(__instance.currentMagazineItem);
        int entityAvailableCount = bagAmmoCount + inventoryAmmoCount;

        // Use our custom ammo checking logic that includes storage
        __result = ItemTexture.ItemTexture_checkAmmo(entityAvailableCount, _actionData, __instance.currentMagazineItem);
        return false; // Skip original method
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(ItemActionTextureBlock.decreaseAmmo))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool ItemActionTextureBlock_decreaseAmmo_Prefix(ItemActionTextureBlock __instance, ItemActionData _actionData, ref bool __result)
    {
        // Handle infinite ammo and creative modes first (same as original)
        if (__instance.InfiniteAmmo || GameStats.GetInt(EnumGameStats.GameModeId) == 2 || GameStats.GetInt(EnumGameStats.GameModeId) == 8)
        {
            __result = true;
            return false; // Skip original method
        }

        // Get the action data and paint cost (same as original)
        ItemActionTextureBlockData textureBlockData = (ItemActionTextureBlockData)_actionData;
        int paintCost = BlockTextureData.list[textureBlockData.idx].PaintCost;

        EntityAlive holdingEntity = _actionData.invData.holdingEntity;
        ItemValue ammoType = __instance.currentMagazineItem;

        // Calculate entity-held ammo (same as original)
        int bagAmmoCount = holdingEntity.bag.GetItemCount(ammoType);
        int inventoryAmmoCount = holdingEntity.inventory.GetItemCount(ammoType);
        int entityAvailableCount = bagAmmoCount + inventoryAmmoCount;

        // Get total available count including storage
        int totalAvailableCount = ItemTexture.ItemTexture_GetAmmoCount(ammoType, entityAvailableCount);

        // Check if we have enough total ammo
        if (totalAvailableCount < paintCost)
        {
            __result = false;
            return false; // Skip original method
        }

        // Remove ammo from entity inventory first (same priority as original)
        int remainingNeeded = paintCost;
        remainingNeeded -= holdingEntity.bag.DecItem(ammoType, remainingNeeded);

        if (remainingNeeded > 0)
        {
            remainingNeeded -= holdingEntity.inventory.DecItem(ammoType, remainingNeeded);
        }

        // Remove any remaining needed from storage
        if (remainingNeeded > 0)
        {
            ItemTexture.ItemTexture_RemoveAmmo(ammoType, remainingNeeded, false, null);
        }

        __result = true;
        return false; // Skip original method
    }
}