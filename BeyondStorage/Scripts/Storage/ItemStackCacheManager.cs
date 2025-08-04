using System;
using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage;

/// <summary>
/// Manages ItemStack caching with a master unfiltered cache and filtered views.
/// All filtered caches are views on the master cache, not separate data stores.
/// </summary>
public class ItemStackCacheManager
{
    private const double ITEMSTACK_CACHE_DURATION = 0.5;
    private const int LRU_SUBFILTER_MAX = 10;
    private const int LRU_SUBFILTER_DISPLAY_MAX = LRU_SUBFILTER_MAX >> 1;

    private static long s_globalInvalidationCounter = 0;

    // Master cache - always unfiltered, contains all discovered items
    private bool _masterCacheValid = false;
    private DateTime _masterCacheTime = DateTime.MinValue;
    private long _masterCacheInvalidationCounter = 0;

    // Subfilter view tracking - these are logical views, not data caches
    private readonly Dictionary<UniqueItemTypes, FilteredCacheView> _filteredViews = [];

    /// <summary>
    /// Represents a filtered view on the master cache.
    /// This is not a data cache - it's just metadata about access time.
    /// </summary>
    private class FilteredCacheView
    {
        public DateTime LastAccessTime { get; set; } = DateTime.Now;

        public FilteredCacheView() { }
    }

    /// <summary>
    /// Gets whether the master cache contains all items (always true when valid).
    /// </summary>
    public bool IsFiltered => false; // Master cache is always unfiltered

    /// <summary>
    /// Gets the current filter types of the master cache (always unfiltered).
    /// </summary>
    public UniqueItemTypes CurrentFilterTypes => UniqueItemTypes.Unfiltered;

    /// <summary>
    /// Checks if we have valid cached data for the specified filter types.
    /// </summary>
    /// <param name="filterTypes">The filter types to check against</param>
    /// <returns>True if we can satisfy the request from cached data</returns>
    public bool IsCachedForFilter(UniqueItemTypes filterTypes)
    {
        filterTypes ??= UniqueItemTypes.Unfiltered;

        if (!IsMasterCacheValid())
        {
            return false;
        }

        // Consistently track all filter access for accurate LRU
        if (filterTypes.IsFiltered)
        {
            CreateOrUpdateFilteredView(filterTypes);
        }

        return true;
    }

    /// <summary>
    /// Checks if we have valid cached data for the specified filter item.
    /// </summary>
    /// <param name="filterItem">The item to check against</param>
    /// <returns>True if we can satisfy the request from cached data</returns>
    public bool IsCachedForFilter(ItemValue filterItem)
    {
        var filterTypes = UniqueItemTypes.FromItemValue(filterItem);
        return IsCachedForFilter(filterTypes);
    }

    /// <summary>
    /// Marks the master cache as valid. Since we always cache unfiltered data,
    /// this also creates or updates a filtered view if a specific filter was requested.
    /// </summary>
    /// <param name="filterTypes">The filter types that were requested (for view tracking)</param>
    public void MarkCached(UniqueItemTypes filterTypes)
    {
        // Master cache is always marked as unfiltered (we discover everything)
        _masterCacheValid = true;
        _masterCacheTime = DateTime.Now;
        _masterCacheInvalidationCounter = s_globalInvalidationCounter;

        // If a specific filter was requested, create/update a view for it
        filterTypes ??= UniqueItemTypes.Unfiltered;
        if (filterTypes.IsFiltered)
        {
            CreateOrUpdateFilteredView(filterTypes);
        }

        ModLogger.DebugLog($"Master cache marked valid. Filter view for {filterTypes} updated.");
    }

    /// <summary>
    /// Gets filtered data on-demand from the master cache.
    /// This method should be called by StorageSourceItemDataStore.GetAllItemStacks().
    /// </summary>
    /// <param name="filterTypes">The filter to apply</param>
    /// <param name="allItemStacks">All item stacks from the master cache</param>
    /// <returns>Filtered view of the item stacks</returns>
    public IEnumerable<ItemStack> GetFilteredView(UniqueItemTypes filterTypes, IEnumerable<ItemStack> allItemStacks)
    {
        filterTypes ??= UniqueItemTypes.Unfiltered;

        // If unfiltered, return everything
        if (filterTypes.IsUnfiltered)
        {
            return allItemStacks;
        }

        // Update view access time
        CreateOrUpdateFilteredView(filterTypes);
        // Apply filter on-demand
        return allItemStacks.Where(stack => filterTypes.Contains(stack));
    }

    /// <summary>
    /// Invalidates the master cache and all filtered views.
    /// </summary>
    public void InvalidateCache()
    {
        _masterCacheValid = false;
        _masterCacheTime = DateTime.MinValue;
        _masterCacheInvalidationCounter = s_globalInvalidationCounter;

        // Clear all filtered views since they depend on the master cache
        _filteredViews.Clear();

        ModLogger.DebugLog("Master cache and all filtered views invalidated");
    }

    /// <summary>
    /// Clears the cache and resets all cache state.
    /// </summary>
    public void ClearCache()
    {
        InvalidateCache(); // Same behavior as invalidate for this design
    }

    /// <summary>
    /// Creates or updates a filtered view entry.
    /// </summary>
    /// <param name="filterTypes">The filter types for the view</param>
    private void CreateOrUpdateFilteredView(UniqueItemTypes filterTypes)
    {
        if (_filteredViews.TryGetValue(filterTypes, out var existingView))
        {
            existingView.LastAccessTime = DateTime.Now;
        }
        else
        {
            _filteredViews[filterTypes] = new FilteredCacheView();
        }

        // Clean up old views periodically (keep most recent LRU_SUBFILTER_MAX)
        if (_filteredViews.Count > LRU_SUBFILTER_MAX)
        {
            // More explicit about how many to remove
            var excessCount = _filteredViews.Count - LRU_SUBFILTER_MAX;
            var oldestKeys = _filteredViews
                .OrderBy(kvp => kvp.Value.LastAccessTime)
                .Take(excessCount)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var oldKey in oldestKeys)
            {
                _filteredViews.Remove(oldKey);
            }
        }
    }

    /// <summary>
    /// Checks if the master cache is currently valid.
    /// </summary>
    /// <returns>True if master cache is valid</returns>
    private bool IsMasterCacheValid()
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

        var viewCount = _filteredViews.Count;
        var viewInfo = viewCount > 0 ? $", {viewCount} filtered views" : "";

        return $"ItemStacks: Master cached {cacheAge:F3}s ago (unfiltered), valid:{isValid}{viewInfo}";
    }

    /// <summary>
    /// Gets diagnostic information about filtered views.
    /// </summary>
    /// <returns>String containing filtered view information</returns>
    public string GetFilteredViewsInfo()
    {
        if (_filteredViews.Count == 0)
        {
            return "No filtered views";
        }

        var viewDetails = _filteredViews
            .OrderByDescending(kvp => kvp.Value.LastAccessTime)
            .Take(LRU_SUBFILTER_DISPLAY_MAX)
            .Select(kvp => $"{kvp.Key}(age:{(DateTime.Now - kvp.Value.LastAccessTime).TotalSeconds:F1}s)")
            .ToList();

        return $"Filtered views ({_filteredViews.Count}): {string.Join(", ", viewDetails)}";
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
    /// Increments the global invalidation counter, invalidating all cache instances.
    /// </summary>
    public static void InvalidateGlobalCache()
    {
        s_globalInvalidationCounter++;
        ModLogger.DebugLog($"Global ItemStack cache invalidated (counter: {s_globalInvalidationCounter})");
    }

    /// <summary>
    /// Gets the current global invalidation counter value.
    /// </summary>
    /// <returns>The current global invalidation counter</returns>
    public static long GetGlobalInvalidationCounter()
    {
        return s_globalInvalidationCounter;
    }
}