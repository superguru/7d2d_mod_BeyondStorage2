using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BeyondStorage.Scripts.Server;
using BeyondStorage.Scripts.Utils;

namespace BeyondStorage.Scripts.ContainerLogic;

public static class ContainerUtils
{
    public static ConcurrentDictionary<Vector3i, int> LockedTileEntities { get; private set; }

    // Statistics tracker for performance monitoring
    private static readonly MethodCallStatistics s_methodStats = new("ContainerUtils");

    public static void Init()
    {
        ServerUtils.HasServerConfig = false;
        LockedTileEntities = new ConcurrentDictionary<Vector3i, int>();
        s_methodStats.Clear();
    }

    public static void Cleanup()
    {
        ServerUtils.HasServerConfig = false;
        LockedTileEntities?.Clear();
        s_methodStats.Clear();
    }

    public static void UpdateLockedTEs(Dictionary<Vector3i, int> lockedTileEntities)
    {
        LockedTileEntities = new ConcurrentDictionary<Vector3i, int>(lockedTileEntities);
        LogUtil.DebugLog($"UpdateLockedTEs: newCount {lockedTileEntities.Count}");
    }

    /// <summary>
    /// Gets call statistics for ContainerUtils methods.
    /// </summary>
    /// <returns>Dictionary of method names and their timing statistics</returns>
    public static Dictionary<string, (int callCount, long totalTimeMs, double avgTimeMs)> GetCallStatistics()
    {
        return s_methodStats.GetAllStatistics();
    }

    /// <summary>
    /// Gets formatted call statistics for logging/debugging.
    /// </summary>
    /// <returns>Formatted string with call statistics</returns>
    public static string GetFormattedCallStatistics()
    {
        return s_methodStats.GetFormattedStatistics();
    }

    private static void AddValidItemStacksFromSources<T>(
            string d_MethodName,
            List<ItemStack> output,
            IEnumerable<T> sources,
            Func<T, ItemStack[]> getStacks,
            string sourceName,
            out int itemsAddedCount,
            ItemValue filterItem = null) where T : class
    {
        itemsAddedCount = 0;

        if (sources == null)
        {
            LogUtil.Error($"{d_MethodName}: {sourceName} pulled in 0 stacks (null source)");
            return;
        }

        int filterType = filterItem?.type ?? -1;
        var filtering = filterType >= 0;

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

                // Apply filter if specified
                if (filtering && stack.itemValue?.type != filterType)
                {
                    continue;
                }

                output.Add(stack);
                itemsAddedCount += stackCount;
            }
        }
    }

    public static List<ItemStack> GetPullableSourceItemStacks(StorageAccessContext context, out int totalItemsAddedCount, ItemValue filterItem = null)
    {
        const string d_MethodName = nameof(GetPullableSourceItemStacks);

        totalItemsAddedCount = 0;

        // Early exit for error conditions - don't track timing for these
        if (context == null)
        {
            context = StorageAccessContext.Create(d_MethodName);
        }

        if (context == null)
        {
            LogUtil.Error($"{d_MethodName}: Failed to create StorageAccessContext");
            return [];
        }

        // Check if we have cached ItemStack results for this filter
        if (context.IsCachedForFilter(filterItem))
        {
            totalItemsAddedCount = context.GetTotalItemCount();
            var cachedResult = context.GetAllItemStacks();

            LogUtil.DebugLog($"{d_MethodName}: Using cached ItemStacks, found {totalItemsAddedCount} items from {cachedResult.Count} stacks - DC:{context.DewCollectorItems.Count}, WS:{context.WorkstationItems.Count}, CT:{context.ContainerItems.Count}, VH:{context.VehicleItems.Count} | {context.GetItemStackCacheInfo()}");

            return cachedResult;
        }

        // Start timing using internal stopwatch management
        s_methodStats.StartTiming(d_MethodName);

        var config = context.Config;

        // Clear any existing ItemStack lists to ensure fresh results
        context.ClearItemStacks();

        if (config.PullFromDewCollectors)
        {
            AddValidItemStacksFromSources(d_MethodName, context.DewCollectorItems, context.DewCollectors, dc => dc.items,
                "Dew Collector Storage", out int dewCollectorItemsAddedCount, filterItem);

            totalItemsAddedCount += dewCollectorItemsAddedCount;
        }

        if (config.PullFromWorkstationOutputs)
        {
            AddValidItemStacksFromSources(d_MethodName, context.WorkstationItems, context.Workstations, workstation => workstation.output,
                "Workstation Output", out int workstationItemsAddedCount, filterItem);

            totalItemsAddedCount += workstationItemsAddedCount;
        }

        // Containers (Lootables)
        AddValidItemStacksFromSources(d_MethodName, context.ContainerItems, context.Lootables, l => l.items,
            "Container Storage", out int containerItemsAddedCount, filterItem);

        totalItemsAddedCount += containerItemsAddedCount;

        if (config.PullFromVehicleStorage)
        {
            AddValidItemStacksFromSources(d_MethodName, context.VehicleItems, context.Vehicles, v => v.bag?.GetSlots(),
                "Vehicle Storage", out int vehicleItemsAddedCount, filterItem);

            totalItemsAddedCount += vehicleItemsAddedCount;
        }

        // Mark the results as cached for this filter
        context.MarkItemStacksCached(filterItem);

        // Get the concatenated result from the context
        var result = context.GetAllItemStacks();

        // Stop timing and explicitly record the call
        var elapsedNs = s_methodStats.StopAndRecordCall(d_MethodName);

        // Get current statistics for logging - now we have accurate data since we just recorded
        var methodStats = s_methodStats.GetMethodStats(d_MethodName);
        var currentAvgNs = methodStats?.avgTimeNs ?? 0;
        var callCount = methodStats?.callCount ?? 1;
        var totaltimeNs = methodStats?.totalTimeNs ?? 0;

        // Format timing using the centralized formatting method
        var currentTimeDisplay = MethodCallStatistics.FormatNanoseconds(elapsedNs);
        var avgTimeDisplay = MethodCallStatistics.FormatNanoseconds(currentAvgNs);
        var totaltimeDisplay = MethodCallStatistics.FormatNanoseconds(totaltimeNs);

        LogUtil.DebugLog($"{d_MethodName}: Exec time: {currentTimeDisplay} (avg: {avgTimeDisplay} over {callCount} calls, cumulative {totaltimeDisplay}), found {totalItemsAddedCount} items from {result.Count} stacks - DC:{context.DewCollectorItems.Count}, WS:{context.WorkstationItems.Count}, CT:{context.ContainerItems.Count}, VH:{context.VehicleItems.Count} | {context.GetItemStackCacheInfo()}");

        return result;
    }

    public static bool HasItem(StorageAccessContext context, ItemValue itemValue)
    {
        const string d_MethodName = nameof(HasItem);

        if (itemValue == null)
        {
            LogUtil.Error($"{d_MethodName} | itemValue is null");
            return false;
        }

        if (context == null)
        {
            context = StorageAccessContext.Create(d_MethodName);
        }

        if (context == null)
        {
            LogUtil.Error($"{d_MethodName}: Failed to create BatchRemovalContext");
            return false;
        }

        var totalItemCount = GetItemCount(context, itemValue);
        var result = totalItemCount > 0;

        LogUtil.DebugLog($"{d_MethodName} for '{itemValue?.ItemClass?.Name}' is {result}");

        return result;
    }

    public static int GetItemCount(StorageAccessContext context, ItemValue itemValue)
    {
        const string d_MethodName = nameof(GetItemCount);

        if (itemValue == null)
        {
            LogUtil.Error($"{d_MethodName} | itemValue is null");
            return 0;
        }

        if (context == null)
        {
            context = StorageAccessContext.Create(d_MethodName);
        }

        if (context == null)
        {
            LogUtil.Error($"{d_MethodName}: Failed to create BatchRemovalContext");
            return 0;
        }

        var sources = GetPullableSourceItemStacks(context, out var totalItemCountAdded, filterItem: itemValue);

        LogUtil.DebugLog($"{d_MethodName} | Found {totalItemCountAdded} of '{itemValue.ItemClass?.Name}'");

        return totalItemCountAdded;
    }

    public static int RemoveRemaining(ItemValue itemValue, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        const string d_MethodName = nameof(RemoveRemaining);

        var context = StorageAccessContext.Create(d_MethodName);
        if (context == null)
        {
            LogUtil.Error($"{d_MethodName}: Failed to create BatchRemovalContext");
            return 0;
        }

        int removedCount = RemoveRemainingWithContext(context, itemValue, stillNeeded, ignoreModdedItems, removedItems);

        return removedCount;
    }

    public static int RemoveRemainingWithContext(StorageAccessContext context, ItemValue itemValue, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        const string d_MethodName = nameof(RemoveRemainingWithContext);

        if (stillNeeded <= 0 || itemValue == null || itemValue.ItemClass == null || itemValue.type <= 0)
        {
#if DEBUG
            LogUtil.Error($"{d_MethodName} | stillNeeded {stillNeeded}; item null is {itemValue == null}");
#endif
            return 0;
        }

        var itemName = itemValue.ItemClass.GetItemName();
        LogUtil.DebugLog($"{d_MethodName} | Trying to remove {stillNeeded} {itemName}");

        int originalNeeded = stillNeeded;

        var config = context.Config;

        if (stillNeeded > 0 && config.PullFromDewCollectors)
        {
            RemoveItemsFromStorageInternal(d_MethodName, "DewCollectors", itemName, context.DewCollectors, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                dewCollector => dewCollector.items, dewCollector => DewCollectorUtils.MarkDewCollectorModified(dewCollector));
        }

        if (stillNeeded > 0 && config.PullFromWorkstationOutputs)
        {
            RemoveItemsFromStorageInternal(d_MethodName, "WorkstationOutputs", itemName, context.Workstations, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                workstation => workstation.output, workstation => WorkstationUtils.MarkWorkstationModified(workstation));
        }

        if (stillNeeded > 0)
        {
            RemoveItemsFromStorageInternal(d_MethodName, "Containers", itemName, context.Lootables, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                lootable => lootable.items, lootable => lootable.SetModified());
        }

        if (stillNeeded > 0 && config.PullFromVehicleStorage)
        {
            RemoveItemsFromStorageInternal(d_MethodName, "Vehicles", itemName, context.Vehicles, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                vehicle => vehicle.bag.items, vehicle => vehicle.SetBagModified());
        }

        return originalNeeded - stillNeeded;  // Return the total number of items removed
    }

    private static void RemoveItemsFromStorageInternal<T>(
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
        LogUtil.DebugLog($"{d_method_name} | {storageName} | Removed {removed} {itemName}, stillNeeded {stillNeeded}");

#if DEBUG
        if (stillNeeded < 0)
        {
            LogUtil.Error($"{d_method_name} | stillNeeded after {storageName} should not be negative, but is {stillNeeded}");
            stillNeeded = 0;
        }
#endif
    }

    private static int RemoveItemsInternal(IEnumerable<ItemStack> items, ItemValue desiredItem, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        // When we get here, desiredItem is definitely not null or empty, so we can safely use it.

        int filterType = desiredItem.type;
        bool itemCanStack = ItemClass.GetForId(filterType).CanStack();

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

            if (ignoreModdedItems && itemValue.HasModSlots && itemValue.HasMods())
            {
                continue;
            }

            if (itemCanStack)
            {
                var countToRemove = Math.Min(stack.count, stillNeeded);
#if DEBUG
                //LogUtil.DebugLog($"RemoveItems Item Count Before: {stack.count} Count To Remove: {countToRemove}");
#endif
                removedItems?.Add(new ItemStack(itemValue.Clone(), countToRemove));
                stack.count -= countToRemove;
                stillNeeded -= countToRemove;
#if DEBUG
                //LogUtil.DebugLog($"RemoveItems Item Count After: {stack.count} Count Still Required {stillNeeded}");
#endif
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
}