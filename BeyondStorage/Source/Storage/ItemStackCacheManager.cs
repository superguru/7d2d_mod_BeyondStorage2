using System;
using BeyondStorage.Scripts.Data;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Manages timing and validation for the master ItemStack cache.
/// The actual item stack storage and filtering is handled by StorageSourceItemDataStore.
/// </summary>
public class ItemStackCacheManager
{
    private const double ITEMSTACK_CACHE_DURATION = 0.5;

    private static long s_globalInvalidationCounter = 0;

    // Master cache timing - always unfiltered, contains all discovered items
    private bool _masterCacheValid = false;
    private DateTime _masterCacheTime = DateTime.MinValue;
    private long _masterCacheInvalidationCounter = 0;

    /// <summary>
    /// Gets whether the master cache contains all items (always true when valid).
    /// </summary>
    public bool IsFiltered => false; // Master cache is always unfiltered

    /// <summary>
    /// Gets the current filter types of the master cache (always unfiltered).
    /// </summary>
    public UniqueItemTypes CurrentFilterTypes => UniqueItemTypes.Unfiltered;

    /// <summary>
    /// Marks the master cache as valid after item discovery.
    /// </summary>
    /// <param name="filterTypes">The filter types that were requested (ignored - we always cache unfiltered)</param>
    public void MarkCached()
    {
        // filterTypes is ignored since we always discover and cache everything unfiltered
        _masterCacheValid = true;
        _masterCacheTime = DateTime.Now;
        _masterCacheInvalidationCounter = s_globalInvalidationCounter;
#if DEBUG
        //ModLogger.DebugLog("Master cache marked valid (unfiltered data cached)");
#endif
    }

    /// <summary>
    /// Invalidates the master cache timing.
    /// The actual data clearing is handled by StorageSourceItemDataStore.
    /// </summary>
    public void InvalidateCache()
    {
        _masterCacheValid = false;
        _masterCacheTime = DateTime.MinValue;
        _masterCacheInvalidationCounter = s_globalInvalidationCounter;
#if DEBUG
        //ModLogger.DebugLog("Master cache timing invalidated");
#endif
    }

    /// <summary>
    /// Increments the global invalidation counter, invalidating all cache instances.
    /// </summary>
    public static void InvalidateGlobalCache()
    {
        s_globalInvalidationCounter++;
#if DEBUG
        //ModLogger.DebugLog($"Global ItemStack cache invalidated (counter: {s_globalInvalidationCounter})");
#endif
    }

    /// <summary>
    /// Checks if a global invalidation has occurred since the last cache update.
    /// </summary>
    /// <returns>True if global invalidation has occurred</returns>
    private bool HasGlobalInvalidationOccurred()
    {
        return s_globalInvalidationCounter != _masterCacheInvalidationCounter;
    }

    /// <summary>
    /// Gets the current global invalidation counter value.
    /// </summary>
    /// <returns>The current global invalidation counter</returns>
    public static long GetGlobalInvalidationCounter()
    {
        return s_globalInvalidationCounter;
    }

    /// <summary>
    /// Clears the cache and resets all cache state.
    /// </summary>
    public void ClearCache()
    {
        InvalidateCache(); // Same behavior as invalidate for this design
    }

    /// <summary>
    /// Checks if the master cache is currently valid based on timing and global invalidation.
    /// </summary>
    /// <returns>True if master cache is valid</returns>
    public bool IsMasterCacheValid()
    {
        if (!_masterCacheValid)
        {
            return false;
        }

        if (HasGlobalInvalidationOccurred())
        {
            InvalidateCache();
            return false;
        }

        var cacheAge = (DateTime.Now - _masterCacheTime).TotalSeconds;
        if (cacheAge > ITEMSTACK_CACHE_DURATION)
        {
            InvalidateCache();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets detailed information about the current cache state.
    /// </summary>
    /// <returns>String containing cache information</returns>
    public string GetCacheInfo()
    {
        if (!_masterCacheValid)
        {
            return "ItemStacks: Master cache not valid";
        }

        var cacheAge = (DateTime.Now - _masterCacheTime).TotalSeconds;
        var isValid = IsMasterCacheValid(); // This handles global invalidation

        return $"ItemStacks: Master cached {cacheAge:F3}s ago (unfiltered), valid:{isValid}";
    }

    /// <summary>
    /// Gets diagnostic information about filtered views. 
    /// Note: Filtered views are now managed by StorageSourceItemDataStore.
    /// </summary>
    /// <returns>String indicating filtered views are managed elsewhere</returns>
    public string GetFilteredViewsInfo()
    {
        return "Filtered views managed by StorageSourceItemDataStore";
    }
}