using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.Storage;

namespace BeyondStorage.Scripts.Game.Item;

public class ItemTexture
{
    // Simple tracking if needed
    private static readonly Dictionary<int, int> s_paintRemovals = [];

    public static bool ItemTexture_checkAmmo(int entityAvailableCount, ItemActionData _actionData, ItemValue ammoType)
    {
        const string d_MethodName = nameof(ItemTexture_checkAmmo);

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Starting");
#endif
        // Paint cost is 1 for everything in v2.x
        if (entityAvailableCount > 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: Entity has available count {entityAvailableCount}, no need to check ammo");
            return true;
        }

        if (!ModConfig.EnableForBlockTexture())
        {
            return false;
        }

        // Use StorageContext directly for efficient checking
        var storageContext = StorageContextFactory.Create(d_MethodName);
        var hasAmmo = storageContext?.HasItem(ammoType) ?? false;

        ModLogger.DebugLog($"{d_MethodName}: StorageContext is {storageContext != null}, hasAmmo is {hasAmmo} for ammoType {ammoType?.ItemClass?.Name}");
        return hasAmmo;
    }

    public static int ItemTexture_GetAmmoCount(ItemValue ammoType, int entityAvailableCount)
    {
        const string d_MethodName = nameof(ItemTexture_GetAmmoCount);

        if (entityAvailableCount < 0)
        {
            entityAvailableCount = 0;
        }

        // Check if feature is enabled
        if (!ModConfig.EnableForBlockTexture())
        {
            return entityAvailableCount;
        }

        // Use StorageContext directly for efficient item counting
        var storageContext = StorageContextFactory.Create(d_MethodName);
        var storageCount = storageContext?.GetItemCount(ammoType) ?? 0;
        var totalAvailableCount = storageCount + entityAvailableCount;

        ModLogger.DebugLog($"{d_MethodName}: StorageContext is {storageContext != null}, storageCount {storageCount}, entityAvailableCount {entityAvailableCount}, total {totalAvailableCount}");
        return totalAvailableCount;
    }

    public static int ItemTexture_RemoveAmmo(ItemValue ammoType, int paintCost, bool _ignoreModdedItems = false, IList<ItemStack> _removedItems = null)
    {
        const string d_MethodName = nameof(ItemTexture_RemoveAmmo);

        // Early exit conditions
        if (paintCost <= 0)
        {
            return paintCost;
        }

        if (!ModConfig.EnableForBlockTexture())
        {
            return paintCost;
        }

        // Use StorageContext directly for efficient removal operations
        var storageContext = StorageContextFactory.Create(d_MethodName);
        var removedFromStorage = storageContext?.RemoveRemaining(ammoType, paintCost, _ignoreModdedItems, _removedItems) ?? 0;
        var stillNeeded = paintCost - removedFromStorage;

        // Invalidate paint caches if needed
        if (removedFromStorage > 0)
        {
            s_paintRemovals.TryGetValue(ammoType.type, out var current);
            s_paintRemovals[ammoType.type] = current + removedFromStorage;
        }

        ModLogger.DebugLog($"{d_MethodName}: StorageContext is {storageContext != null}, ammoType {ammoType?.ItemClass?.Name}, paintCost {paintCost}, removedFromStorage {removedFromStorage}, stillNeeded {stillNeeded}");
        return removedFromStorage;
    }
}