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

        if (itemValue == null)
        {
            ModLogger.Warning($"{d_MethodName}: itemValue is null, returning false");
            return false;
        }

        var context = StorageContextFactory.Create(d_MethodName);

        if (!context.Config.EnableForReload)
        {
            return false;
        }

        var canReloadFromStorage = context.HasItem(itemValue);

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: {itemValue.ItemClass.Name} {canReloadFromStorage}");
#endif
        return canReloadFromStorage;
    }

    // Used By:
    //      AnimatorRangedReloadState.GetAmmoCount (Weapon Reload - Get Total Ammo (not displayed))
    //      Animator3PRangedReloadState.GetAmmoCount (Weapon Reload - Get Total Ammo (not displayed))
    public static int GetAmmoCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(GetAmmoCount);

        if (itemValue == null)
        {
            ModLogger.Warning($"{d_MethodName}: itemValue is null, returning 0");
            return 0;
        }

        var context = StorageContextFactory.Create(d_MethodName);
        return context.GetItemCount(itemValue);
    }

    // Used By:
    //      AnimatorRangedReloadState.GetAmmoCountToReload (Weapon Reload - ClearStacksForFilter Items For Reload)
    //      Animator3PRangedReloadState.GetAmmoCountToReload (Weapon Reload - ClearStacksForFilter Items For Reload)
    public static int RemoveAmmoForReload(ItemValue ammoType, bool isPerMag, int maxMagSize, int currentAmmo)
    {
        const string d_MethodName = nameof(RemoveAmmoForReload);

        // This is also called when refuelling something like an augur when there is nothing in the player inventory

        if (ammoType == null)
        {
            ModLogger.Warning($"{d_MethodName}: ammoType is null or empty, returning 0");
            return 0;
        }

        var context = StorageContextFactory.Create(d_MethodName);

        // return 0 if not enabled for reloading
        if (context.Config.EnableForReload)
        {
            return 0;
        }

        var ammoRequired = isPerMag ? 1 : maxMagSize - currentAmmo;
        var ammoRemovedFromStorage = context.RemoveRemaining(ammoType, ammoRequired);
#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: {ammoType.ItemClass.GetItemName()} isPerMag {isPerMag}; maxMagSize {maxMagSize}; currentAmnmo {currentAmmo}; ammoRemovedFromStorage {ammoRemovedFromStorage};");
#endif
        return isPerMag ? maxMagSize * ammoRemovedFromStorage : ammoRemovedFromStorage;
    }
}