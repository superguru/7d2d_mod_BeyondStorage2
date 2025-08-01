using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;

namespace BeyondStorage.Scripts.Storage
{
    /// <summary>
    /// Service responsible for extracting ItemStacks from storage sources with filtering and caching support.
    /// </summary>
    public static class ItemStackExtractionService
    {
        /// <summary>
        /// Extracts ItemStacks for a specific item type.
        /// </summary>
        /// <param name="sources">The storage sources to extract from</param>
        /// <param name="config">Configuration settings for extraction</param>
        /// <param name="filterItem">The specific item to filter for</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <returns>Total count of items extracted</returns>
        public static int ExtractItemStacks(StorageSourceManager sources, ConfigSnapshot config, ItemValue filterItem, ItemStackCacheManager cacheManager)
        {
            var filterTypes = filterItem != null
                ? UniqueItemTypes.FromItemType(filterItem.type)
                : UniqueItemTypes.Unfiltered;

            return ExtractItemStacks(sources, config, filterTypes, cacheManager);
        }

        /// <summary>
        /// Extracts ItemStacks from storage sources, applying filtering and caching logic.
        /// </summary>
        /// <param name="sources">The storage sources to extract from</param>
        /// <param name="config">Configuration settings for extraction</param>
        /// <param name="filterTypes">Filter types to apply during extraction</param>
        /// <param name="cacheManager">Cache manager for tracking cache state</param>
        /// <returns>Total count of items extracted</returns>
        public static int ExtractItemStacks(StorageSourceManager sources, ConfigSnapshot config, UniqueItemTypes filterTypes, ItemStackCacheManager cacheManager)
        {
            const string d_MethodName = nameof(ExtractItemStacks);

            var totalItemCountAdded = 0;
            filterTypes ??= UniqueItemTypes.Unfiltered;

            if (cacheManager.IsCachedForFilter(filterTypes))
            {
                totalItemCountAdded = CountCachedItems(sources);
                ModLogger.DebugLog($"{d_MethodName}: Using cached ItemStacks, found {totalItemCountAdded} items from {GetTotalStackCount(sources)} stacks - DC:{sources.DewCollectorItems.Count}, WS:{sources.WorkstationItems.Count}, CT:{sources.LootableItems.Count}, VH:{sources.VehicleItems.Count} | {cacheManager.GetCacheInfo()}");

                cacheManager.MarkCached(filterTypes);
                return totalItemCountAdded;
            }

            sources.ClearItemStacks();
            cacheManager.ClearCache();

            if (config.PullFromDewCollectors)
            {
                AddValidItemStacksFromSources(d_MethodName, sources.DewCollectorItems, sources.DewCollectors, dc => dc.items,
                    "Dew Collector", out int dewCollectorItemsAddedCount, filterTypes);
                totalItemCountAdded += dewCollectorItemsAddedCount;
            }

            if (config.PullFromWorkstationOutputs)
            {
                AddValidItemStacksFromSources(d_MethodName, sources.WorkstationItems, sources.Workstations, workstation => workstation.output,
                    "Workstation Output", out int workstationItemsAddedCount, filterTypes);
                totalItemCountAdded += workstationItemsAddedCount;
            }

            AddValidItemStacksFromSources(d_MethodName, sources.LootableItems, sources.Lootables, l => l.items,
                "Container Storage", out int containerItemsAddedCount, filterTypes);
            totalItemCountAdded += containerItemsAddedCount;

            if (config.PullFromVehicleStorage)
            {
                AddValidItemStacksFromSources(d_MethodName, sources.VehicleItems, sources.Vehicles, v => v.bag?.GetSlots(),
                    "Vehicle Storage", out int vehicleItemsAddedCount, filterTypes);
                totalItemCountAdded += vehicleItemsAddedCount;
            }

            cacheManager.MarkCached(filterTypes);

            ModLogger.DebugLog($"{d_MethodName}: Found {totalItemCountAdded} items from {GetTotalStackCount(sources)} stacks - DC:{sources.DewCollectorItems.Count}, WS:{sources.WorkstationItems.Count}, CT:{sources.LootableItems.Count}, VH:{sources.VehicleItems.Count} | {cacheManager.GetCacheInfo()}");
            return totalItemCountAdded;
        }

        /// <summary>
        /// Adds valid ItemStacks from a collection of sources to the output list, applying filtering.
        /// </summary>
        /// <typeparam name="T">The type of storage source</typeparam>
        /// <param name="d_MethodName">Calling method name for logging</param>
        /// <param name="output">The list to add valid ItemStacks to</param>
        /// <param name="sources">The collection of storage sources</param>
        /// <param name="getStacks">Function to get ItemStack array from a source</param>
        /// <param name="sourceName">Name of the source type for logging</param>
        /// <param name="itemsAddedCount">Output parameter for total items added</param>
        /// <param name="filterTypes">Optional filter to limit results to specific item types</param>
        public static void AddValidItemStacksFromSources<T>(
            string d_MethodName,
            List<ItemStack> output,
            IEnumerable<T> sources,
            Func<T, ItemStack[]> getStacks,
            string sourceName,
            out int itemsAddedCount,
            UniqueItemTypes filterTypes = null) where T : class
        {
            itemsAddedCount = 0;
            filterTypes ??= UniqueItemTypes.Unfiltered;

            if (sources == null)
            {
                ModLogger.Warning($"{d_MethodName}: {sourceName} collection is null, skipping");
                return;
            }

            int nullSourceCount = 0;
            int nullStackArrayCount = 0;

            foreach (var source in sources)
            {
                if (source == null)
                {
                    nullSourceCount++;
                    continue;
                }

                var stacks = getStacks(source);
                if (stacks == null)
                {
                    nullStackArrayCount++;
                    continue;
                }

                for (int i = 0; i < stacks.Length; i++)
                {
                    var stack = stacks[i];
                    int stackCount = stack?.count ?? 0;

                    if (stackCount <= 0)
                    {
                        continue;
                    }

                    var itemValue = stack.itemValue;
                    if (itemValue?.ItemClass == null)
                    {
                        continue; // Skip invalid items
                    }

                    int itemType = itemValue.type;
                    if (itemType <= 0)
                    {
                        continue; // Skip invalid item types
                    }

                    if (filterTypes.IsFiltered && !filterTypes.Contains(itemType))
                    {
                        continue;
                    }

                    output.Add(stack);
                    itemsAddedCount += stackCount;
                }
            }

            if (nullSourceCount > 0 || nullStackArrayCount > 0)
            {
                ModLogger.DebugLog($"{d_MethodName}: {sourceName} - Skipped {nullSourceCount} null sources and {nullStackArrayCount} null stack arrays");
            }
        }

        /// <summary>
        /// Gets all available ItemStacks from sources in a single combined list.
        /// </summary>
        /// <param name="sources">The storage sources to get ItemStacks from</param>
        /// <returns>Combined list of all ItemStacks</returns>
        public static List<ItemStack> GetAllItemStacks(StorageSourceManager sources)
        {
            var totalStacks = GetTotalStackCount(sources);
            var result = new List<ItemStack>(totalStacks);

            result.AddRange(sources.DewCollectorItems);
            result.AddRange(sources.WorkstationItems);
            result.AddRange(sources.LootableItems);
            result.AddRange(sources.VehicleItems);

            return result;
        }

        /// <summary>
        /// Counts the total number of items across all cached ItemStacks.
        /// </summary>
        /// <param name="sources">The storage sources to count items from</param>
        /// <returns>Total count of all items</returns>
        public static int CountCachedItems(StorageSourceManager sources)
        {
            int total = 0;

            foreach (var stack in sources.DewCollectorItems)
            {
                total += stack?.count ?? 0;
            }

            foreach (var stack in sources.WorkstationItems)
            {
                total += stack?.count ?? 0;
            }

            foreach (var stack in sources.LootableItems)
            {
                total += stack?.count ?? 0;
            }

            foreach (var stack in sources.VehicleItems)
            {
                total += stack?.count ?? 0;
            }

            return total;
        }

        /// <summary>
        /// Gets the total number of ItemStack instances across all storage sources.
        /// </summary>
        /// <param name="sources">The storage sources to count stacks from</param>
        /// <returns>Total number of ItemStack instances</returns>
        public static int GetTotalStackCount(StorageSourceManager sources)
        {
            return sources.DewCollectorItems.Count + sources.WorkstationItems.Count +
                   sources.LootableItems.Count + sources.VehicleItems.Count;
        }

        /// <summary>
        /// Creates detailed extraction statistics for debugging and monitoring.
        /// </summary>
        /// <param name="sources">The storage sources to analyze</param>
        /// <param name="cacheManager">The cache manager to get cache info from</param>
        /// <returns>String containing detailed extraction statistics</returns>
        public static string GetExtractionStats(StorageSourceManager sources, ItemStackCacheManager cacheManager)
        {
            var itemCount = CountCachedItems(sources);
            var stackCount = GetTotalStackCount(sources);
            var cacheInfo = cacheManager.IsCachedForFilter(cacheManager.CurrentFilterTypes) ? "cached" : "not cached";
            var filterStatus = cacheManager.CurrentFilterTypes.IsFiltered
                ? $"filtered ({cacheManager.CurrentFilterTypes.Count} types)"
                : "unfiltered";

            return $"Extraction stats: {cacheInfo}, {filterStatus}, {itemCount} items in {stackCount} stacks - DC:{sources.DewCollectorItems.Count}, WS:{sources.WorkstationItems.Count}, CT:{sources.LootableItems.Count}, VH:{sources.VehicleItems.Count}";
        }
    }
}