using System;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage
{
    /// <summary>
    /// Manages ItemStack caching with filtering support and global invalidation tracking.
    /// </summary>
    public class ItemStackCacheManager
    {
        private bool _itemStacksCached = false;
        private DateTime _itemStacksCacheTime = DateTime.MinValue;
        private long _itemStacksInvalidationCounter = 0;
        private UniqueItemTypes _lastFilterTypes = UniqueItemTypes.Unfiltered;

        private const double ITEMSTACK_CACHE_DURATION = 0.5;
        private static long s_globalInvalidationCounter = 0;

        /// <summary>
        /// Gets whether the cache is currently filtered.
        /// </summary>
        public bool IsFiltered => _lastFilterTypes.IsFiltered;

        /// <summary>
        /// Gets the current filter types applied to the cache.
        /// </summary>
        public UniqueItemTypes CurrentFilterTypes => _lastFilterTypes;

        /// <summary>
        /// Checks if the cache is valid for the specified filter types.
        /// </summary>
        /// <param name="filterTypes">The filter types to check against</param>
        /// <returns>True if the cache is valid for the filter types</returns>
        public bool IsCachedForFilter(UniqueItemTypes filterTypes)
        {
            if (!_itemStacksCached)
            {
                return false;
            }

            if (HasGlobalInvalidationOccurred())
            {
                InvalidateCache();
                return false;
            }

            var cacheAge = (DateTime.Now - _itemStacksCacheTime).TotalSeconds;
            if (cacheAge > ITEMSTACK_CACHE_DURATION)
            {
                _itemStacksCached = false;
                return false;
            }

            if (ReferenceEquals(_lastFilterTypes, filterTypes))
            {
                return true;
            }

            return UniqueItemTypes.IsEquivalent(_lastFilterTypes, filterTypes);
        }

        /// <summary>
        /// Checks if the cache is valid for the specified filter item.
        /// </summary>
        /// <param name="filterItem">The item to check against</param>
        /// <returns>True if the cache is valid for the filter item</returns>
        public bool IsCachedForFilter(ItemValue filterItem)
        {
            var filterTypes = filterItem != null
                ? UniqueItemTypes.FromItemType(filterItem.type)
                : UniqueItemTypes.Unfiltered;

            return IsCachedForFilter(filterTypes);
        }

        /// <summary>
        /// Marks the cache as valid for the specified filter types.
        /// </summary>
        /// <param name="filterTypes">The filter types to mark as cached</param>
        public void MarkCached(UniqueItemTypes filterTypes)
        {
            _lastFilterTypes = filterTypes ?? UniqueItemTypes.Unfiltered;
            _itemStacksCached = true;
            _itemStacksCacheTime = DateTime.Now;
            _itemStacksInvalidationCounter = s_globalInvalidationCounter;
        }

        /// <summary>
        /// Invalidates the current cache state.
        /// </summary>
        public void InvalidateCache()
        {
            _itemStacksCached = false;
            _lastFilterTypes = UniqueItemTypes.Unfiltered;
            _itemStacksCacheTime = DateTime.MinValue;
            _itemStacksInvalidationCounter = s_globalInvalidationCounter;
        }

        /// <summary>
        /// Clears the cache and resets all cache state.
        /// </summary>
        public void ClearCache()
        {
            _itemStacksCached = false;
            _lastFilterTypes = UniqueItemTypes.Unfiltered;
            _itemStacksCacheTime = DateTime.MinValue;
            _itemStacksInvalidationCounter = s_globalInvalidationCounter;
        }

        /// <summary>
        /// Gets detailed information about the current cache state.
        /// </summary>
        /// <returns>String containing cache information</returns>
        public string GetCacheInfo()
        {
            if (!_itemStacksCached)
            {
                return "ItemStacks: Not cached";
            }

            var cacheAge = (DateTime.Now - _itemStacksCacheTime).TotalSeconds;
            var isValid = cacheAge <= ITEMSTACK_CACHE_DURATION && !HasGlobalInvalidationOccurred();

            var filterInfo = _lastFilterTypes.IsFiltered
                ? $"filtered for {_lastFilterTypes.Count} types"
                : "unfiltered (all items)";

            var globalInvalidationInfo = HasGlobalInvalidationOccurred() ? ", globally invalidated" : "";

            return $"ItemStacks: Cached {cacheAge:F2}s ago, {filterInfo}, valid:{isValid}{globalInvalidationInfo}";
        }

        /// <summary>
        /// Checks if a global invalidation has occurred since the last cache update.
        /// </summary>
        /// <returns>True if global invalidation has occurred</returns>
        private bool HasGlobalInvalidationOccurred()
        {
            return s_globalInvalidationCounter != _itemStacksInvalidationCounter;
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
}