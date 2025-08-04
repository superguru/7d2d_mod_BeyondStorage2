using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Ranged;

public static class Ranged
{
    // Used By:
    //      ItemActionRanged.CanReload (Weapon Reload - Ammo Exists Check)
    public static bool CanReloadFromStorage(ItemValue itemValue)
    {
        const string d_MethodName = nameof(CanReloadFromStorage);

        if (!ModConfig.EnableForReload())
        {
            return false;
        }

        var context = StorageContextFactory.Create(d_MethodName);
        var canReloadFromStorage = context?.HasItem(itemValue) ?? false;

        ModLogger.DebugLog($"{d_MethodName}: {canReloadFromStorage}");
        return canReloadFromStorage;
    }

    // TODO: SetStacksForFilter this to return early if we hit the max ammo for mag
    // Used By:
    //      AnimatorRangedReloadState.GetAmmoCount (Weapon Reload - GetStacksForFilter Total Ammo LRU_SUBFILTER_DISPLAY_MAX (not displayed))
    //      Animator3PRangedReloadState.GetAmmoCount (Weapon Reload - GetStacksForFilter Total Ammo LRU_SUBFILTER_DISPLAY_MAX (not displayed))
    public static int GetAmmoCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(GetAmmoCount);

        var context = StorageContextFactory.Create(d_MethodName);
        return context?.GetItemCount(itemValue) ?? 0;
    }

    // Used By:
    //      AnimatorRangedReloadState.GetAmmoCountToReload (Weapon Reload - ClearStacksForFilter Items For Reload)
    //      Animator3PRangedReloadState.GetAmmoCountToReload (Weapon Reload - ClearStacksForFilter Items For Reload)
    public static int RemoveAmmoForReload(ItemValue ammoType, bool isPerMag, int maxMagSize, int currentAmmo)
    {
        const string d_MethodName = nameof(RemoveAmmoForReload);

        // This is also called when refuelling something like an augur when there is nothing in the player inventory

        // return 0 if not enabled for reloading
        if (!ModConfig.EnableForReload())
        {
            return 0;
        }

        var ammoRequired = isPerMag ? 1 : maxMagSize - currentAmmo;
        var context = StorageContextFactory.Create(d_MethodName);
        var ammoRemovedFromStorage = context?.RemoveRemaining(ammoType, ammoRequired) ?? 0;

        ModLogger.DebugLog($"{d_MethodName} {ammoType.ItemClass.GetItemName()} isPerMag {isPerMag}; maxMagSize {maxMagSize}; currentAmnmo {currentAmmo}; ammoRemovedFromStorage {ammoRemovedFromStorage};");
        return isPerMag ? maxMagSize * ammoRemovedFromStorage : ammoRemovedFromStorage;
    }
}