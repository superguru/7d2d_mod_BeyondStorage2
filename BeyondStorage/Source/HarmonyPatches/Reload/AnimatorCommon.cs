using System;
using BeyondStorage.Scripts.Game.Ranged;

namespace BeyondStorage.HarmonyPatches.Reload;

public static class AnimatorCommon
{
    public static int GetAmmoCount(ItemValue ammoType, int lastResult, int maxAmmo)
    {
        return maxAmmo == lastResult ? lastResult : Math.Min(Ranged.GetAmmoCount(ammoType) + lastResult, maxAmmo);
    }

    /// <summary>
    /// Replicated logic from AnimatorRangedReloadState.GetAmmoCountToReload with storage integration.
    /// This method removes ammo from player inventories and storage, returning the amount to reload into the weapon.
    /// </summary>
    /// <param name="actionRanged">The ranged action instance</param>
    /// <param name="actionData">The ranged action data instance</param>
    /// <param name="ea">The entity performing the reload</param>
    /// <param name="ammo">The ammo item value to remove</param>
    /// <param name="modifiedMagazineSize">The modified magazine size</param>
    /// <returns>The amount of ammo to add to the weapon magazine</returns>
    public static int RemoveAndCountAmmoForReload(ItemActionRanged actionRanged, ItemActionRanged.ItemActionDataRanged actionData, EntityAlive ea, ItemValue ammo, int modifiedMagazineSize)
    {
        // Handle infinite ammo case (original logic)
        if (actionRanged.HasInfiniteAmmo(actionData))
        {
            if (actionRanged.AmmoIsPerMagazine)
            {
                return modifiedMagazineSize;
            }
            return modifiedMagazineSize - actionData.invData.itemValue.Meta;
        }

        // Calculate how much ammo is needed
        int stillNeeded = actionRanged.AmmoIsPerMagazine ? 1 : (modifiedMagazineSize - actionData.invData.itemValue.Meta);
        int totalAmmoRemoved = 0;

        // Step 1: Try to remove ammo from bag first
        int removed = ea.bag.DecItem(ammo, stillNeeded);
        totalAmmoRemoved += removed;
        stillNeeded -= removed;

        // Step 2: If still need more, try toolbelt inventory
        if (stillNeeded > 0)
        {
            removed = actionData.invData.holdingEntity.inventory.DecItem(ammo, stillNeeded);
            totalAmmoRemoved += removed;
            stillNeeded -= removed;
        }

        // Step 3: If still need more, try storage
        if (stillNeeded > 0)
        {
            int currentAmmo = actionData.invData.itemValue.Meta + totalAmmoRemoved;
            int storageReloadAmount = Ranged.RemoveAmmoForReload(ammo, actionRanged.AmmoIsPerMagazine, modifiedMagazineSize, currentAmmo);

            // Add the reload amount directly to our total
            if (actionRanged.AmmoIsPerMagazine)
            {
                return modifiedMagazineSize * totalAmmoRemoved + storageReloadAmount;
            }
            else
            {
                return totalAmmoRemoved + storageReloadAmount;
            }
        }

        // Calculate final reload amount based on ammo type
        if (actionRanged.AmmoIsPerMagazine)
        {
            // For per-magazine weapons: return full magazine worth for each ammo item consumed
            return modifiedMagazineSize * totalAmmoRemoved;
        }
        else
        {
            // For per-bullet weapons: return actual ammo consumed
            return totalAmmoRemoved;
        }
    }
}