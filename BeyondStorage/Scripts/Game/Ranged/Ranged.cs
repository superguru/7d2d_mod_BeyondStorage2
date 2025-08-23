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
        const bool DEFAULT_RETURN_VALUE = false;

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, out StorageContext context, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        var canReloadFromStorage = context.HasItem(itemValue);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: {itemName} {canReloadFromStorage}");
#endif
        return canReloadFromStorage;
    }

    // Used By:
    //      AnimatorRangedReloadState.GetAmmoCount (Weapon Reload - Get Total Ammo (not displayed))
    //      Animator3PRangedReloadState.GetAmmoCount (Weapon Reload - Get Total Ammo (not displayed))
    public static int GetAmmoCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(GetAmmoCount);
        const int DEFAULT_RETURN_VALUE = 0;

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, out StorageContext context, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        return context.GetItemCount(itemValue);
    }

    // Used By:
    //      AnimatorRangedReloadState.GetAmmoCountToReload (Weapon Reload - Remove Items For Reload)
    //      Animator3PRangedReloadState.GetAmmoCountToReload (Weapon Reload - Remove Items For Reload)
    public static int RemoveAmmoForReload(ItemValue itemValue, bool isPerMag, int maxMagSize, int currentAmmo)
    {
        const string d_MethodName = nameof(RemoveAmmoForReload);
        const int DEFAULT_RETURN_VALUE = 0;

        // This is also called when refuelling something like an augur when there is nothing in the player inventory

        if (!ValidationHelper.ValidateItemAndContext(itemValue, d_MethodName, out StorageContext context, out string itemName))
        {
            return DEFAULT_RETURN_VALUE;
        }

        var ammoRequired = isPerMag ? 1 : maxMagSize - currentAmmo;
        var ammoRemovedFromStorage = context.RemoveRemaining(itemValue, ammoRequired);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: {itemName} isPerMag {isPerMag}; maxMagSize {maxMagSize}; currentAmmo {currentAmmo}; ammoRemovedFromStorage {ammoRemovedFromStorage};");
#endif
        return isPerMag ? maxMagSize * ammoRemovedFromStorage : ammoRemovedFromStorage;
    }
}