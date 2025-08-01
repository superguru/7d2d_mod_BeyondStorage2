using System;
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
        public static int GetItemCount(StorageSourceManager sources, ConfigSnapshot config, ItemStackCacheManager cacheManager, ItemValue itemValue)
        {
            const string d_MethodName = nameof(GetItemCount);

            if (!ValidateParameters(sources, config, cacheManager, d_MethodName))
            {
                return 0;
            }

            if (itemValue == null)
            {
                ModLogger.Error($"{d_MethodName} | itemValue is null");
                return 0;
            }

            try
            {
                var totalItemCountAdded = ItemStackExtractionService.ExtractItemStacks(sources, config, itemValue, cacheManager);
                ModLogger.DebugLog($"{d_MethodName} | Found {totalItemCountAdded} of '{itemValue.ItemClass?.Name}'");
                return totalItemCountAdded;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"{d_MethodName} | Exception occurred: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the count of items matching the specified filter types.
        /// </summary>
        /// <param name="sources">The storage sources to query</param>
        /// <param name="config">Configuration for which storage types to include</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <param name="filterTypes">The filter types to count</param>
        /// <returns>Total count of items matching the filter</returns>
        public static int GetItemCount(StorageSourceManager sources, ConfigSnapshot config, ItemStackCacheManager cacheManager, UniqueItemTypes filterTypes)
        {
            const string d_MethodName = nameof(GetItemCount);

            if (!ValidateParameters(sources, config, cacheManager, d_MethodName))
            {
                return 0;
            }

            filterTypes ??= UniqueItemTypes.Unfiltered;

            try
            {
                var totalItemCountAdded = ItemStackExtractionService.ExtractItemStacks(sources, config, filterTypes, cacheManager);
                ModLogger.DebugLog($"{d_MethodName} | Found {totalItemCountAdded} items with filter: {filterTypes}");
                return totalItemCountAdded;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"{d_MethodName} | Exception occurred: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Checks if a specific item is available in storage.
        /// </summary>
        /// <param name="sources">The storage sources to query</param>
        /// <param name="config">Configuration for which storage types to include</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <param name="itemValue">The item to check for</param>
        /// <returns>True if the item is available in storage</returns>
        public static bool HasItem(StorageSourceManager sources, ConfigSnapshot config, ItemStackCacheManager cacheManager, ItemValue itemValue)
        {
            const string d_MethodName = nameof(HasItem);

            if (!ValidateParameters(sources, config, cacheManager, d_MethodName))
            {
                return false;
            }

            if (itemValue == null)
            {
                ModLogger.Error($"{d_MethodName} | itemValue is null");
                return false;
            }

            try
            {
                var totalItemCount = GetItemCount(sources, config, cacheManager, itemValue);
                var result = totalItemCount > 0;
                ModLogger.DebugLog($"{d_MethodName} for '{itemValue?.ItemClass?.Name}' is {result}");
                return result;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"{d_MethodName} | Exception occurred: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if items matching the filter types are available in storage.
        /// </summary>
        /// <param name="sources">The storage sources to query</param>
        /// <param name="config">Configuration for which storage types to include</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <param name="filterTypes">The filter types to check for</param>
        /// <returns>True if items matching the filter are available</returns>
        public static bool HasItem(StorageSourceManager sources, ConfigSnapshot config, ItemStackCacheManager cacheManager, UniqueItemTypes filterTypes)
        {
            const string d_MethodName = nameof(HasItem);

            if (!ValidateParameters(sources, config, cacheManager, d_MethodName))
            {
                return false;
            }

            filterTypes ??= UniqueItemTypes.Unfiltered;

            try
            {
                var totalItemCount = GetItemCount(sources, config, cacheManager, filterTypes);
                var result = totalItemCount > 0;
                ModLogger.DebugLog($"{d_MethodName} with filter: {filterTypes} is {result}");
                return result;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"{d_MethodName} | Exception occurred: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets all available item stacks from storage sources with optional filtering.
        /// </summary>
        /// <param name="sources">The storage sources to query</param>
        /// <param name="config">Configuration for which storage types to include</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <param name="filterTypes">Optional filter to limit results to specific item types</param>
        /// <returns>List of all available item stacks from storage sources</returns>
        public static List<ItemStack> GetAllAvailableItemStacks(StorageSourceManager sources, ConfigSnapshot config, ItemStackCacheManager cacheManager, UniqueItemTypes filterTypes)
        {
            const string d_MethodName = nameof(GetAllAvailableItemStacks);

            if (!ValidateParameters(sources, config, cacheManager, d_MethodName))
            {
                return new List<ItemStack>();
            }

            try
            {
                ItemStackExtractionService.ExtractItemStacks(sources, config, filterTypes, cacheManager);
                return ItemStackExtractionService.GetAllItemStacks(sources);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"{d_MethodName} | Exception occurred: {ex.Message}");
                return new List<ItemStack>();
            }
        }

        /// <summary>
        /// Gets the total count of all items across all storage sources.
        /// </summary>
        /// <param name="sources">The storage sources to query</param>
        /// <param name="config">Configuration for which storage types to include</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <returns>Total count of all items</returns>
        public static int GetTotalItemCount(StorageSourceManager sources, ConfigSnapshot config, ItemStackCacheManager cacheManager)
        {
            const string d_MethodName = nameof(GetTotalItemCount);

            if (!ValidateParameters(sources, config, cacheManager, d_MethodName))
            {
                return 0;
            }

            try
            {
                ItemStackExtractionService.ExtractItemStacks(sources, config, cacheManager.CurrentFilterTypes, cacheManager);
                return ItemStackExtractionService.CountCachedItems(sources);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"{d_MethodName} | Exception occurred: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the total number of ItemStack instances across all storage sources.
        /// </summary>
        /// <param name="sources">The storage sources to query</param>
        /// <param name="config">Configuration for which storage types to include</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <returns>Total number of ItemStack instances</returns>
        public static int GetTotalStackCount(StorageSourceManager sources, ConfigSnapshot config, ItemStackCacheManager cacheManager)
        {
            const string d_MethodName = nameof(GetTotalStackCount);

            if (!ValidateParameters(sources, config, cacheManager, d_MethodName))
            {
                return 0;
            }

            try
            {
                ItemStackExtractionService.ExtractItemStacks(sources, config, cacheManager.CurrentFilterTypes, cacheManager);
                return ItemStackExtractionService.GetTotalStackCount(sources);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"{d_MethodName} | Exception occurred: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Validates common parameters used by all query methods.
        /// </summary>
        /// <param name="sources">Storage sources to validate</param>
        /// <param name="config">Configuration to validate</param>
        /// <param name="cacheManager">Cache manager to validate</param>
        /// <param name="methodName">Calling method name for logging</param>
        /// <returns>True if all parameters are valid</returns>
        private static bool ValidateParameters(StorageSourceManager sources, ConfigSnapshot config, ItemStackCacheManager cacheManager, string methodName)
        {
            if (sources == null)
            {
                ModLogger.Error($"{methodName} | sources is null");
                return false;
            }

            if (config == null)
            {
                ModLogger.Error($"{methodName} | config is null");
                return false;
            }

            if (cacheManager == null)
            {
                ModLogger.Error($"{methodName} | cacheManager is null");
                return false;
            }

            return true;
        }
    }
}