using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage
{
    /// <summary>
    /// Service responsible for querying storage sources for item availability and counts.
    /// Provides read-only operations for checking what items are available in storage.
    /// </summary>
    public static class StorageQueryService
    {
        /// <summary>
        /// Gets the count of a specific item type across all storage sources.
        /// </summary>
        /// <param name="sources">The storage sources to query</param>
        /// <param name="config">Configuration for which storage types to include</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <param name="itemValue">The item to count</param>
        /// <returns>Total count of the specified item</returns>
        public static int GetItemCount(StorageSourceCollection sources, ConfigSnapshot config, ItemStackCacheManager cacheManager, ItemValue itemValue)
        {
            const string d_MethodName = nameof(GetItemCount);

            if (itemValue == null)
            {
                ModLogger.Error($"{d_MethodName} | itemValue is null");
                return 0;
            }

            var totalItemCountAdded = ItemStackExtractionService.ExtractItemStacks(sources, config, itemValue, cacheManager);

            ModLogger.DebugLog($"{d_MethodName} | Found {totalItemCountAdded} of '{itemValue.ItemClass?.Name}'");

            return totalItemCountAdded;
        }

        /// <summary>
        /// Gets the count of items matching the specified filter types.
        /// </summary>
        /// <param name="sources">The storage sources to query</param>
        /// <param name="config">Configuration for which storage types to include</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <param name="filterTypes">The filter types to count</param>
        /// <returns>Total count of items matching the filter</returns>
        public static int GetItemCount(StorageSourceCollection sources, ConfigSnapshot config, ItemStackCacheManager cacheManager, UniqueItemTypes filterTypes)
        {
            const string d_MethodName = nameof(GetItemCount);

            filterTypes ??= UniqueItemTypes.Unfiltered;

            var totalItemCountAdded = ItemStackExtractionService.ExtractItemStacks(sources, config, filterTypes, cacheManager);

            ModLogger.DebugLog($"{d_MethodName} | Found {totalItemCountAdded} items with filter: {filterTypes}");
            return totalItemCountAdded;
        }

        /// <summary>
        /// Checks if a specific item is available in storage.
        /// </summary>
        /// <param name="sources">The storage sources to query</param>
        /// <param name="config">Configuration for which storage types to include</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <param name="itemValue">The item to check for</param>
        /// <returns>True if the item is available in storage</returns>
        public static bool HasItem(StorageSourceCollection sources, ConfigSnapshot config, ItemStackCacheManager cacheManager, ItemValue itemValue)
        {
            const string d_MethodName = nameof(HasItem);

            if (itemValue == null)
            {
                ModLogger.Error($"{d_MethodName} | itemValue is null");
                return false;
            }

            var totalItemCount = GetItemCount(sources, config, cacheManager, itemValue);
            var result = totalItemCount > 0;

            ModLogger.DebugLog($"{d_MethodName} for '{itemValue?.ItemClass?.Name}' is {result}");
            return result;
        }

        /// <summary>
        /// Checks if items matching the filter types are available in storage.
        /// </summary>
        /// <param name="sources">The storage sources to query</param>
        /// <param name="config">Configuration for which storage types to include</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <param name="filterTypes">The filter types to check for</param>
        /// <returns>True if items matching the filter are available</returns>
        public static bool HasItem(StorageSourceCollection sources, ConfigSnapshot config, ItemStackCacheManager cacheManager, UniqueItemTypes filterTypes)
        {
            const string d_MethodName = nameof(HasItem);

            filterTypes ??= UniqueItemTypes.Unfiltered;

            var totalItemCount = GetItemCount(sources, config, cacheManager, filterTypes);
            var result = totalItemCount > 0;

            ModLogger.DebugLog($"{d_MethodName} with filter: {filterTypes} is {result}");

            return result;
        }

        /// <summary>
        /// Gets all available item stacks from storage sources with optional filtering.
        /// </summary>
        /// <param name="sources">The storage sources to query</param>
        /// <param name="config">Configuration for which storage types to include</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <param name="filterTypes">Optional filter to limit results to specific item types</param>
        /// <returns>List of all available item stacks from storage sources</returns>
        public static List<ItemStack> GetAllAvailableItemStacks(StorageSourceCollection sources, ConfigSnapshot config, ItemStackCacheManager cacheManager, UniqueItemTypes filterTypes)
        {
            ItemStackExtractionService.ExtractItemStacks(sources, config, filterTypes, cacheManager);
            return ItemStackExtractionService.GetAllItemStacks(sources);
        }

        /// <summary>
        /// Gets the total count of all items across all storage sources.
        /// </summary>
        /// <param name="sources">The storage sources to query</param>
        /// <param name="config">Configuration for which storage types to include</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <returns>Total count of all items</returns>
        public static int GetTotalItemCount(StorageSourceCollection sources, ConfigSnapshot config, ItemStackCacheManager cacheManager)
        {
            // Ensure ItemStacks are pulled with current filter
            ItemStackExtractionService.ExtractItemStacks(sources, config, cacheManager.CurrentFilterTypes, cacheManager);
            return ItemStackExtractionService.CountCachedItems(sources);
        }

        /// <summary>
        /// Gets the total number of ItemStack instances across all storage sources.
        /// </summary>
        /// <param name="sources">The storage sources to query</param>
        /// <param name="config">Configuration for which storage types to include</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <returns>Total number of ItemStack instances</returns>
        public static int GetTotalStackCount(StorageSourceCollection sources, ConfigSnapshot config, ItemStackCacheManager cacheManager)
        {
            // Ensure ItemStacks are pulled with current filter
            ItemStackExtractionService.ExtractItemStacks(sources, config, cacheManager.CurrentFilterTypes, cacheManager);
            return ItemStackExtractionService.GetTotalStackCount(sources);
        }
    }
}