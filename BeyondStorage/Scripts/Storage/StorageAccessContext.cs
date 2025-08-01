using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Caching;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Game;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.TileEntities;

namespace BeyondStorage.Scripts.Storage;

public sealed class StorageAccessContext
{
    private const double DEFAULT_CACHE_DURATION = 0.5;

    private static readonly ExpiringCache<StorageAccessContext> s_contextCache = new(DEFAULT_CACHE_DURATION, nameof(StorageAccessContext));

    private ConfigSnapshot Config { get; }
    private WorldPlayerContext WorldPlayerContext { get; }
    private StorageSourceCollection Sources { get; }
    private ItemStackCacheManager CacheManager { get; }

    private DateTime CreatedAt { get; }

    // Legacy properties for backward compatibility - delegate to Sources
    private List<TileEntityDewCollector> DewCollectors => Sources.DewCollectors;
    private List<ITileEntityLootable> Lootables => Sources.Lootables;
    private List<EntityVehicle> Vehicles => Sources.Vehicles;
    private List<TileEntityWorkstation> Workstations => Sources.Workstations;

    private List<ItemStack> DewCollectorItems => Sources.DewCollectorItems;
    private List<ItemStack> WorkstationItems => Sources.WorkstationItems;
    private List<ItemStack> ContainerItems => Sources.ContainerItems;
    private List<ItemStack> VehicleItems => Sources.VehicleItems;

    // Legacy properties for backward compatibility - delegate to CacheManager
    public bool IsFiltered => CacheManager.IsFiltered;
    public UniqueItemTypes CurrentFilterTypes => CacheManager.CurrentFilterTypes;

    public static StorageAccessContext Create(string methodName = "Unknown", bool forceRefresh = false)
    {
        return s_contextCache.GetOrCreate(() => CreateFresh(methodName), forceRefresh, methodName);
    }

    private static StorageAccessContext CreateFresh(string methodName)
    {
        try
        {
            var context = new StorageAccessContext();

            if (context.WorldPlayerContext == null)
            {
                ModLogger.Error($"{methodName}: Created StorageAccessContext with null WorldPlayerContext");
                return null;
            }

            ModLogger.DebugLog($"{methodName}: Created fresh StorageAccessContext with {context.GetSourceSummary()}");
            return context;
        }
        catch (Exception ex)
        {
            ModLogger.Error($"{methodName}: Exception creating StorageAccessContext: {ex.Message}");
            return null;
        }
    }

    private StorageAccessContext()
    {
        Config = ConfigSnapshot.Current;
        Sources = new StorageSourceCollection();
        CacheManager = new ItemStackCacheManager();

        WorldPlayerContext = WorldPlayerContext.TryCreate(nameof(StorageAccessContext));
        if (WorldPlayerContext == null)
        {
            ModLogger.Error($"{nameof(StorageAccessContext)}: Failed to create WorldPlayerContext, aborting context creation.");
            CreatedAt = DateTime.Now;
            return;
        }

        StorageDiscoveryService.DiscoverStorageSources(Sources, WorldPlayerContext, Config);

        CreatedAt = DateTime.Now;
        ModLogger.DebugLog($"StorageAccessContext created: {Lootables.Count} lootables, {DewCollectors.Count} dew collectors, {Workstations.Count} workstations, {Vehicles.Count} vehicles");
    }

    public bool IsCachedForFilter(UniqueItemTypes filterTypes)
    {
        return CacheManager.IsCachedForFilter(filterTypes);
    }

    public bool IsCachedForFilter(ItemValue filterItem)
    {
        return CacheManager.IsCachedForFilter(filterItem);
    }

    private void MarkItemStacksCached(UniqueItemTypes filterTypes)
    {
        CacheManager.MarkCached(filterTypes);
    }

    private void ClearItemStacks()
    {
        Sources.ClearItemStacks();
        CacheManager.ClearCache();
    }

    private void InvalidateItemStacksCache()
    {
        CacheManager.InvalidateCache();
    }

    public string GetItemStackCacheInfo()
    {
        return CacheManager.GetCacheInfo();
    }

    /// <summary>
    /// Gets the total count of all items across all storage sources.
    /// Ensures ItemStacks are pulled before counting.
    /// </summary>
    /// <returns>Total count of all items</returns>
    public int GetTotalItemCount()
    {
        // Ensure ItemStacks are pulled with current filter
        PullSourceItemStacks(CurrentFilterTypes);

        int total = 0;
        foreach (var stack in DewCollectorItems)
        {
            total += stack.count;
        }

        foreach (var stack in WorkstationItems)
        {
            total += stack.count;
        }

        foreach (var stack in ContainerItems)
        {
            total += stack.count;
        }

        foreach (var stack in VehicleItems)
        {
            total += stack.count;
        }

        return total;
    }

    /// <summary>
    /// Gets the total number of ItemStack instances across all storage sources.
    /// Ensures ItemStacks are pulled before counting.
    /// </summary>
    /// <returns>Total number of ItemStack instances</returns>
    public int GetTotalStackCount()
    {
        // Ensure ItemStacks are pulled with current filter
        PullSourceItemStacks(CurrentFilterTypes);

        return DewCollectorItems.Count + WorkstationItems.Count + ContainerItems.Count + VehicleItems.Count;
    }

    /// <summary>
    /// Gets all available item stacks from storage sources with filter types.
    /// This is a convenience method that combines PullSourceItemStacks and GetAllItemStacks.
    /// </summary>
    /// <param name="filterTypes">Optional filter to limit results to specific item types</param>
    /// <returns>List of all available item stacks from storage sources</returns>
    public List<ItemStack> GetAllAvailableItemStacks(UniqueItemTypes filterTypes)
    {
        PullSourceItemStacks(filterTypes);

        // Direct access to lists since we just called PullSourceItemStacks
        var totalStacks = DewCollectorItems.Count + WorkstationItems.Count + ContainerItems.Count + VehicleItems.Count;
        var result = new List<ItemStack>(totalStacks);

        result.AddRange(DewCollectorItems);
        result.AddRange(WorkstationItems);
        result.AddRange(ContainerItems);
        result.AddRange(VehicleItems);

        return result;
    }

    /// <summary>
    /// Gets filtering and cache statistics.
    /// Ensures ItemStacks are pulled before generating statistics.
    /// </summary>
    /// <returns>String containing filtering statistics</returns>
    public string GetFilteringStats()
    {
        var cacheInfo = CacheManager.IsCachedForFilter(CurrentFilterTypes) ? "cached" : "not cached";
        var filterStatus = CurrentFilterTypes.IsFiltered
            ? $"filtered ({CurrentFilterTypes.Count} types)"
            : "unfiltered";

        // These methods now ensure ItemStacks are pulled
        var itemCount = GetTotalItemCount();
        var stackCount = GetTotalStackCount();

        return $"Filter stats: {cacheInfo}, {filterStatus}, {itemCount} items in {stackCount} stacks";
    }

    public int GetItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(GetItemCount);

        if (itemValue == null)
        {
            ModLogger.Error($"{d_MethodName} | itemValue is null");
            return 0;
        }

        var totalItemCountAdded = PullSourceItemStacks(itemValue);

        ModLogger.DebugLog($"{d_MethodName} | Found {totalItemCountAdded} of '{itemValue.ItemClass?.Name}'");

        return totalItemCountAdded;
    }

    public int GetItemCount(UniqueItemTypes filterTypes)
    {
        const string d_MethodName = nameof(GetItemCount);

        if (filterTypes == null)
        {
            filterTypes = UniqueItemTypes.Unfiltered;
        }

        var totalItemCountAdded = PullSourceItemStacks(filterTypes);

        ModLogger.DebugLog($"{d_MethodName} | Found {totalItemCountAdded} items with filter: {filterTypes}");
        return totalItemCountAdded;
    }

    public bool HasItem(ItemValue itemValue)
    {
        const string d_MethodName = nameof(HasItem);

        if (itemValue == null)
        {
            ModLogger.Error($"{d_MethodName} | itemValue is null");
            return false;
        }

        var totalItemCount = GetItemCount(itemValue);
        var result = totalItemCount > 0;

        ModLogger.DebugLog($"{d_MethodName} for '{itemValue?.ItemClass?.Name}' is {result}");
        return result;
    }

    public bool HasItem(UniqueItemTypes filterTypes)
    {
        const string d_MethodName = nameof(HasItem);

        if (filterTypes == null)
        {
            filterTypes = UniqueItemTypes.Unfiltered;
        }

        var totalItemCount = GetItemCount(filterTypes);
        var result = totalItemCount > 0;

        ModLogger.DebugLog($"{d_MethodName} with filter: {filterTypes} is {result}");

        return result;
    }

    public int RemoveRemaining(ItemValue itemValue, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        const string d_MethodName = nameof(RemoveRemaining);

        if (stillNeeded <= 0 || itemValue == null || itemValue.ItemClass == null || itemValue.type <= 0)
        {
#if DEBUG
            ModLogger.Error($"{d_MethodName} | stillNeeded {stillNeeded}; item null is {itemValue == null}");
#endif
            return 0;
        }

        var itemName = itemValue.ItemClass.GetItemName();
        ModLogger.DebugLog($"{d_MethodName} | Trying to remove {stillNeeded} {itemName}");

        int originalNeeded = stillNeeded;

        var config = Config;

        if (stillNeeded > 0 && config.PullFromDewCollectors)
        {
            RemoveItemsFromStorageInternal(d_MethodName, "DewCollectors", itemName, DewCollectors, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                dewCollector => dewCollector.items, dewCollector => DewCollectorStateManager.MarkDewCollectorModified(dewCollector));
        }

        if (stillNeeded > 0 && config.PullFromWorkstationOutputs)
        {
            RemoveItemsFromStorageInternal(d_MethodName, "WorkstationOutputs", itemName, Workstations, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                workstation => workstation.output, workstation => WorkstationStateManager.MarkWorkstationModified(workstation));
        }

        if (stillNeeded > 0)
        {
            RemoveItemsFromStorageInternal(d_MethodName, "Containers", itemName, Lootables, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                lootable => lootable.items, lootable => lootable.SetModified());
        }

        if (stillNeeded > 0 && config.PullFromVehicleStorage)
        {
            RemoveItemsFromStorageInternal(d_MethodName, "Vehicles", itemName, Vehicles, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                vehicle => vehicle.bag.items, vehicle => vehicle.SetBagModified());
        }

        return originalNeeded - stillNeeded;
    }

    private void RemoveItemsFromStorageInternal<T>(
        string d_method_name,
        string storageName,
        string itemName,
        List<T> storages,
        ItemValue itemValue,
        ref int stillNeeded,
        bool ignoreModdedItems,
        IList<ItemStack> removedItems,
        Func<T, IEnumerable<ItemStack>> getItems,
        Action<T> markModified)
    {
        int originalNeeded = stillNeeded;

        foreach (var storage in storages)
        {
            if (stillNeeded <= 0)
            {
                break;
            }

            int newNeeded = RemoveItemsInternal(getItems(storage), itemValue, stillNeeded, ignoreModdedItems, removedItems);
            if (stillNeeded != newNeeded)
            {
                markModified(storage);
                stillNeeded = newNeeded;
            }
        }

        int removed = originalNeeded - stillNeeded;
        ModLogger.DebugLog($"{d_method_name} | {storageName} | Removed {removed} {itemName}, stillNeeded {stillNeeded}");

#if DEBUG
        if (stillNeeded < 0)
        {
            ModLogger.Error($"{d_method_name} | stillNeeded after {storageName} should not be negative, but is {stillNeeded}");
            stillNeeded = 0;
        }
#endif
    }

    private int RemoveItemsInternal(IEnumerable<ItemStack> items, ItemValue desiredItem, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        int filterType = desiredItem.type;
        bool itemCanStack = ItemPropertiesCache.GetCanStack(desiredItem);

        foreach (var stack in items)
        {
            if (stillNeeded <= 0)
            {
                break;
            }

            if (stack?.count <= 0)
            {
                continue;
            }

            var itemValue = stack.itemValue;
            if (itemValue?.type != filterType)
            {
                continue;
            }

            if (ItemPropertiesCache.ShouldIgnoreModdedItem(itemValue, ignoreModdedItems))
            {
                continue;
            }

            if (itemCanStack)
            {
                var countToRemove = Math.Min(stack.count, stillNeeded);
                removedItems?.Add(new ItemStack(itemValue.Clone(), countToRemove));
                stack.count -= countToRemove;
                stillNeeded -= countToRemove;
                if (stack.count <= 0)
                {
                    stack.Clear();
                }
            }
            else
            {
                removedItems?.Add(stack.Clone());
                stack.Clear();
                --stillNeeded;
            }
        }

        return stillNeeded;
    }

    private int PullSourceItemStacks(ItemValue filterItem)
    {
        var filterTypes = filterItem != null
            ? UniqueItemTypes.FromItemType(filterItem.type)
            : UniqueItemTypes.Unfiltered;

        var totalItemCountAdded = PullSourceItemStacks(filterTypes);
        return totalItemCountAdded;
    }

    private int PullSourceItemStacks(UniqueItemTypes filterTypes)
    {
        const string d_MethodName = nameof(PullSourceItemStacks);

        var totalItemCountAdded = 0;
        filterTypes ??= UniqueItemTypes.Unfiltered;

        if (CacheManager.IsCachedForFilter(filterTypes))
        {
            totalItemCountAdded = 0;
            foreach (var stack in DewCollectorItems)
            {
                totalItemCountAdded += stack.count;
            }
            foreach (var stack in WorkstationItems)
            {
                totalItemCountAdded += stack.count;
            }
            foreach (var stack in ContainerItems)
            {
                totalItemCountAdded += stack.count;
            }
            foreach (var stack in VehicleItems)
            {
                totalItemCountAdded += stack.count;
            }

            ModLogger.DebugLog($"{d_MethodName}: Using cached ItemStacks, found {totalItemCountAdded} items from {DewCollectorItems.Count + WorkstationItems.Count + ContainerItems.Count + VehicleItems.Count} stacks - DC:{DewCollectorItems.Count}, WS:{WorkstationItems.Count}, CT:{ContainerItems.Count}, VH:{VehicleItems.Count} | {GetItemStackCacheInfo()}");
            return totalItemCountAdded;
        }

        ClearItemStacks();

        if (Config.PullFromDewCollectors)
        {
            AddValidItemStacksFromSources(d_MethodName, DewCollectorItems, DewCollectors, dc => dc.items,
                "Dew Collector Storage", out int dewCollectorItemsAddedCount, filterTypes);

            totalItemCountAdded += dewCollectorItemsAddedCount;
        }

        if (Config.PullFromWorkstationOutputs)
        {
            AddValidItemStacksFromSources(d_MethodName, WorkstationItems, Workstations, workstation => workstation.output,
                "Workstation Output", out int workstationItemsAddedCount, filterTypes);

            totalItemCountAdded += workstationItemsAddedCount;
        }

        AddValidItemStacksFromSources(d_MethodName, ContainerItems, Lootables, l => l.items,
            "Container Storage", out int containerItemsAddedCount, filterTypes);

        totalItemCountAdded += containerItemsAddedCount;

        if (Config.PullFromVehicleStorage)
        {
            AddValidItemStacksFromSources(d_MethodName, VehicleItems, Vehicles, v => v.bag?.GetSlots(),
                "Vehicle Storage", out int vehicleItemsAddedCount, filterTypes);

            totalItemCountAdded += vehicleItemsAddedCount;
        }

        MarkItemStacksCached(filterTypes);

        ModLogger.DebugLog($"{d_MethodName}: Found {totalItemCountAdded} items from {DewCollectorItems.Count + WorkstationItems.Count + ContainerItems.Count + VehicleItems.Count} stacks - DC:{DewCollectorItems.Count}, WS:{WorkstationItems.Count}, CT:{ContainerItems.Count}, VH:{VehicleItems.Count} | {GetItemStackCacheInfo()}");
        return totalItemCountAdded;
    }

    private void AddValidItemStacksFromSources<T>(
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
            ModLogger.Error($"{d_MethodName}: {sourceName} pulled in 0 stacks (null source)");
            return;
        }

        foreach (var source in sources)
        {
            if (source == null)
            {
                continue;
            }

            var stacks = getStacks(source);
            if (stacks == null)
            {
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

                if (filterTypes.IsFiltered && !filterTypes.Contains(stack.itemValue?.type ?? 0))
                {
                    continue;
                }

                output.Add(stack);
                itemsAddedCount += stackCount;
            }
        }
    }

    public static void InvalidateCache()
    {
        s_contextCache.InvalidateCache();
        ItemStackCacheManager.InvalidateGlobalCache();

        ModLogger.DebugLog($"StorageAccessContext cache invalidated");
    }

    public static double GetCacheAge()
    {
        return s_contextCache.GetCacheAge();
    }

    public static bool HasValidCachedContext()
    {
        return s_contextCache.HasValidCachedItem();
    }

    public static string GetCacheStats()
    {
        return s_contextCache.GetCacheStats();
    }

    public double AgeInSeconds => (DateTime.Now - CreatedAt).TotalSeconds;

    public object WorldPlayerContextAgeInSeconds { get { return WorldPlayerContext?.AgeInSeconds ?? -1; } }

    public bool HasExpired(double lifetimeSeconds) => AgeInSeconds > lifetimeSeconds;

    public string GetSourceSummary()
    {
        return $"{Sources.GetSourceSummary()}, Age: {AgeInSeconds:F1}s";
    }

    /// <summary>
    /// Gets a summary of ItemStack information.
    /// Ensures ItemStacks are pulled before generating the summary.
    /// </summary>
    /// <returns>String containing ItemStack summary information</returns>
    public string GetItemStackSummary()
    {
        var cacheInfo = GetItemStackCacheInfo();
        var filterStats = GetFilteringStats();

        // Use direct access since GetFilteringStats() -> GetTotalItemCount/GetTotalStackCount() already called PullSourceItemStacks()
        return $"{Sources.GetItemStackSummary()}, {0} items | {cacheInfo} | {filterStats}";
    }

    public static string GetComprehensiveCacheStats()
    {
        var contextStats = s_contextCache.GetCacheStats();
        var worldPlayerStats = WorldPlayerContext.GetCacheStats();
        var itemPropsStats = ItemPropertiesCache.GetCacheStats();
        var globalInvalidations = ItemStackCacheManager.GetGlobalInvalidationCounter();
        return $"StorageAccessContext: {contextStats} | WorldPlayerContext: {worldPlayerStats} | {itemPropsStats} | Global invalidations: {globalInvalidations}";
    }

    internal static bool IsValidContext(StorageAccessContext context)
    {
        return context != null && context.WorldPlayerContext != null;
    }
}