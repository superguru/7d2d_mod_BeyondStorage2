using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic.Item;

public class ItemTexture
{
    public static bool ItemTexture_checkAmmo(int entityAvailableCount, ItemActionData _actionData, ItemValue ammoType)
    {
        const string d_MethodName = nameof(ItemTexture_checkAmmo);


        // Paint cost is 1 for everything in v2.x
        if (entityAvailableCount > 0)
        {
            return true;
        }

        if (!ModConfig.EnableForBlockTexture())
        {
            return false;
        }

        // Use batch context for efficient checking
        var batchContext = BatchPaintContext.Create(d_MethodName);
        var removalContext = batchContext?.StorageContext;
        var hasAmmo = batchContext != null && removalContext != null;

        if (hasAmmo)
        {
            hasAmmo = ContainerUtils.HasItem(removalContext, ammoType);
        }

        LogUtil.DebugLog($"{d_MethodName}: Batch is {removalContext != null}, hasAmmo is {hasAmmo} for ammoType {ammoType?.ItemClass?.Name}");
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

        // Use batch context for efficient item counting
        var batchContext = BatchPaintContext.Create(d_MethodName);
        var removalContext = batchContext?.StorageContext;

        var storageCount = ContainerUtils.GetItemCount(removalContext, ammoType);
        var totalAvailableCount = storageCount + entityAvailableCount;

        LogUtil.DebugLog($"{d_MethodName}: Batch is {removalContext != null}, storageCount {storageCount}, entityAvailableCount {entityAvailableCount}, total {totalAvailableCount}");
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

        // Use batch context for efficient removal operations
        var batchContext = BatchPaintContext.Create(d_MethodName);
        var removalContext = batchContext?.StorageContext;

        var removedFromStorage = ContainerUtils.RemoveRemainingWithContext(removalContext, ammoType, paintCost, _ignoreModdedItems, _removedItems);
        batchContext?.AccumulateRemoval(ammoType, removedFromStorage);
        var stillNeeded = paintCost - removedFromStorage;

        LogUtil.DebugLog($"{d_MethodName}: Batch is {removalContext != null}, ammoType {ammoType?.ItemClass?.Name}, paintCost {paintCost}, removedFromStorage {removedFromStorage}, stillNeeded {stillNeeded}");
        return removedFromStorage;
    }

    /// <summary>
    /// Forces invalidation of the batch paint cache. 
    /// Call this when paint operations are complete or when world state changes significantly.
    /// </summary>
    public static void InvalidateBatchPaintCache()
    {
        BatchPaintContext.InvalidateCache();
    }

    /// <summary>
    /// Gets batch paint cache statistics for debugging.
    /// </summary>
    public static string GetBatchPaintCacheStats()
    {
        return BatchPaintContext.GetCacheStats();
    }

    /// <summary>
    /// Forces invalidation of all related caches (batch paint and batch removal context).
    /// </summary>
    public static void InvalidateAllCaches()
    {
        BatchPaintContext.InvalidateAllCaches();
    }

    /// <summary>
    /// Gets comprehensive cache statistics for all related caches.
    /// </summary>
    public static string GetComprehensiveCacheStats()
    {
        return BatchPaintContext.GetComprehensiveCacheStats();
    }
}