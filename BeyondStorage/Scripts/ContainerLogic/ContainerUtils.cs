using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Server;
using BeyondStorage.Scripts.Utils;


namespace BeyondStorage.Scripts.ContainerLogic;

public static class ContainerUtils
{
    public const int DEFAULT_DEW_COLLECTOR_LIST_CAPACITY = 16;
    public const int DEFAULT_WORKSTATION_LIST_CAPACITY = 16;
    public const int DEFAULT_LOOTBLE_LIST_CAPACITY = 16;

    public static ConcurrentDictionary<Vector3i, int> LockedTileEntities { get; private set; }

    public static void Init()
    {
        ServerUtils.HasServerConfig = false;
        LockedTileEntities = new ConcurrentDictionary<Vector3i, int>();
    }

    public static void Cleanup()
    {
        ServerUtils.HasServerConfig = false;
        LockedTileEntities?.Clear();
    }

    public static void UpdateLockedTEs(Dictionary<Vector3i, int> lockedTileEntities)
    {
        LockedTileEntities = new ConcurrentDictionary<Vector3i, int>(lockedTileEntities);
        LogUtil.DebugLog($"UpdateLockedTEs: newCount {lockedTileEntities.Count}");
    }

    private static void AddValidItemStacksFromSources<T>(
            string d_MethodName,
            List<ItemStack> output,
            IEnumerable<T> sources,
            Func<T, ItemStack[]> getStacks,
            string sourceName,
            out int itemsAddedCount,
            ref int stillNeeded,
            ItemValue filterItem = null) where T : class
    {
        itemsAddedCount = 0;

        if (sources == null)
        {
            LogUtil.Error($"{d_MethodName}: {sourceName} pulled in 0 stacks (null source)");
            return;
        }

        // stillNeeded == -1 means "get all", which is valid
        // Only error on values < -1
        if (stillNeeded < -1)
        {
            LogUtil.Error($"{d_MethodName}: {sourceName} invalid stillNeeded value: {stillNeeded}. resetting it to 0 and returning.");
            stillNeeded = 0;  // Reset to 0 if negative, as it doesn't make sense
            return;
        }

        int filterType = filterItem?.type ?? -1;
        var filtering = filterType >= 0;

        bool shouldGetAllItems = stillNeeded == -1;
        LogUtil.DebugLog($"{d_MethodName}: {sourceName} filtering is {filtering}, stillNeeded {stillNeeded}, shouldGetAllItems {shouldGetAllItems}");

        foreach (var source in sources)
        {
            if (!shouldGetAllItems && stillNeeded == 0)
            {
                break;  // No need to continue if we already have enough items
            }

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
                if (!shouldGetAllItems && stillNeeded == 0)
                {
                    break;  // No need to continue if we already have enough items
                }

                var stack = stacks[i];
                int stackCount = stack?.count ?? 0;

                if (stackCount <= 0)
                {
                    continue;  // This is just part of the game logic, but we don't want to add empty stacks
                }

                // Apply filter if specified
                if (filtering && stack.itemValue?.type != filterType)
                {
                    continue;
                }

                if (shouldGetAllItems)
                {
                    output.Add(stack);
                    itemsAddedCount += stackCount;
                }
                else
                {
                    if (stackCount <= stillNeeded)
                    {
                        // Add the entire stack to the output
                        output.Add(stack);

                        itemsAddedCount += stackCount;
                        stillNeeded -= stackCount;
                    }
                    else
                    {
                        // Only add a partial stack to the output
                        var partialStack = new ItemStack(stack.itemValue, stillNeeded);
                        output.Add(partialStack);

                        itemsAddedCount += stillNeeded;
                        stillNeeded = 0;
                    }
                }
            }
        }

        LogUtil.DebugLog($"{d_MethodName}: {sourceName} pulled in {itemsAddedCount} items, stillNeeded {stillNeeded}");
    }

    public static List<ItemStack> GetPullableSourceItemStacks(BatchRemovalContext context, out int totalItemsAddedCount, ItemValue filterItem = null, int stillNeeded = -1)
    {
        const string d_MethodName = nameof(GetPullableSourceItemStacks);

        // Start timing for total execution
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        totalItemsAddedCount = 0;

        if (stillNeeded == 0)
        {
            return [];
        }

        if (context == null)
        {
            context = BatchRemovalContext.Create(d_MethodName);
        }

        if (context == null)
        {
            LogUtil.Error($"{d_MethodName}: Failed to create BatchRemovalContext");
            return [];
        }

        //LogUtil.DebugLog($"{d_MethodName}: Found {context.DewCollectors.Count} dew collectors, {context.Workstations.Count} workstations, {context.Lootables.Count} lootables, {context.Vehicles.Count} vehicles, stillNeeded {stillNeeded}");
        var config = context.Config;

        var result = new List<ItemStack>(ItemUtil.DEFAULT_ITEMSTACK_LIST_CAPACITY);

        if (config.PullFromDewCollectors)
        {
            AddValidItemStacksFromSources(d_MethodName, result, context.DewCollectors, dc => dc.items,
                "Dew Collector Storage", out int dewCollectorItemsAddedCount, ref stillNeeded, filterItem);
            //LogUtil.DebugLog($"{d_MethodName}: Found {context.DewCollectors.Count} dew collectors, added {dewCollectorItemsAddedCount} items, stillNeeded {stillNeeded}");

            totalItemsAddedCount += dewCollectorItemsAddedCount;
            if (stillNeeded == 0)
            {
                stopwatch.Stop();
                int elapsedMs = (int)stopwatch.ElapsedMilliseconds;
                LogUtil.DebugLog($"{d_MethodName}: Early exit after dew collectors - Total execution time: {elapsedMs}ms");
                return result;
            }
        }

        if (config.PullFromWorkstationOutputs)
        {
            AddValidItemStacksFromSources(d_MethodName, result, context.Workstations, workstation => workstation.output,
                "Workstation Output", out int workstationItemsAddedCount, ref stillNeeded, filterItem);
            //LogUtil.DebugLog($"{d_MethodName}: Found {context.Workstations.Count} workstations, added {workstationItemsAddedCount} items, stillNeeded {stillNeeded}");

            totalItemsAddedCount += workstationItemsAddedCount;
            if (stillNeeded == 0)
            {
                stopwatch.Stop();
                int elapsedMs = (int)stopwatch.ElapsedMilliseconds;
                LogUtil.DebugLog($"{d_MethodName}: Early exit after workstations - Total execution time: {elapsedMs}ms");
                return result;
            }
        }

        {
            AddValidItemStacksFromSources(d_MethodName, result, context.Lootables, l => l.items,
                "Container Storage", out int containerItemsAddedCount, ref stillNeeded, filterItem);
            //LogUtil.DebugLog($"{d_MethodName}: Found {context.Lootables.Count} containers, added {containerItemsAddedCount} items, stillNeeded {stillNeeded}");

            totalItemsAddedCount += containerItemsAddedCount;
            if (stillNeeded == 0)
            {
                stopwatch.Stop();
                int elapsedMs = (int)stopwatch.ElapsedMilliseconds;
                LogUtil.DebugLog($"{d_MethodName}: Early exit after containers - Total execution time: {elapsedMs}ms");
                return result;
            }
        }

        if (config.PullFromVehicleStorage)
        {
            AddValidItemStacksFromSources(d_MethodName, result, context.Vehicles, v => v.bag?.GetSlots(),
                "Vehicle Storage", out int vehicleItemsAddedCount, ref stillNeeded, filterItem);
            //LogUtil.DebugLog($"{d_MethodName}: Found {context.Vehicles.Count} vehicles, added {vehicleItemsAddedCount} items, stillNeeded {stillNeeded}");

            totalItemsAddedCount += vehicleItemsAddedCount;
        }

        // Stop timing and log the total execution time
        stopwatch.Stop();
        int totalElapsedMs = (int)stopwatch.ElapsedMilliseconds;
        LogUtil.DebugLog($"{d_MethodName}: Total execution time: {totalElapsedMs}ms, found {totalItemsAddedCount} items from {result.Count} stacks");

        return result;
    }

    public static void DiscoverTileEntitySources(BatchRemovalContext context)
    {
        if (context == null)
        {
            LogUtil.Error($"{nameof(DiscoverTileEntitySources)}: context is null, aborting.");
            return;
        }

        AddPullableTileEntities(context);
    }

    private static void AddPullableTileEntities(BatchRemovalContext context)
    {
        const string d_MethodName = nameof(AddPullableTileEntities);

        if (context == null)
        {
            LogUtil.Error($"{d_MethodName}: context is null, aborting.");
            return;
        }

        if (context.WorldPlayerContext == null)
        {
            LogUtil.Error($"{d_MethodName}: WorldPlayerContext is null, aborting.");
            return;
        }

        var config = context.Config;
        var worldPlayerContext = context.WorldPlayerContext;
        var dewCollectors = context.DewCollectors;
        var workstations = context.Workstations;
        var lootables = context.Lootables;

        int chunksProcessed = 0;
        int nullChunks = 0;
        int tileEntitiesProcessed = 0;

        foreach (var chunk in worldPlayerContext.ChunkCacheCopy)
        {
            if (chunk == null)
            {
                nullChunks++;
                continue;
            }

            chunksProcessed++;

            var tileEntityList = chunk.tileEntities?.list;
            if (tileEntityList == null)
            {
                continue;
            }

            foreach (var tileEntity in tileEntityList)
            {
                tileEntitiesProcessed++;

                // Skip if being removed
                if (tileEntity.IsRemoving)
                {
                    continue;
                }

                // 1. Type checks first (cheapest) - cache both interface queries
                bool isLootable = tileEntity.TryGetSelfOrFeature(out ITileEntityLootable lootable);
                bool hasStorageFeature = config.OnlyStorageCrates ? tileEntity.TryGetSelfOrFeature(out TEFeatureStorage _) : true;

                if (!(tileEntity is TileEntityDewCollector ||
                      tileEntity is TileEntityWorkstation ||
                      isLootable))
                {
                    continue;
                }

                // 2. Then positional checks
                var tileEntityWorldPos = tileEntity.ToWorldPos();

                // Locked check (skip if locked by another player)
                if (LockedTileEntities.Count > 0)
                {
                    if (LockedTileEntities.TryGetValue(tileEntityWorldPos, out int entityId) && entityId != worldPlayerContext.PlayerEntityId)
                    {
                        continue;
                    }
                }

                // Range check first for early exit
                if (!worldPlayerContext.IsWithinRange(tileEntityWorldPos, config.Range))
                {
                    continue;
                }

                // Lockable check (skip if locked and not allowed)
                if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
                {
                    if (!worldPlayerContext.CanAccessLockable(tileLockable))
                    {
                        continue;
                    }
                }

                // DEW COLLECTOR check
                if (config.PullFromDewCollectors && tileEntity is TileEntityDewCollector dewCollector)
                {
                    // Skip if any player is currently accessing the dew collector
                    if (dewCollector.bUserAccessing)
                    {
                        continue;
                    }

                    if (dewCollector.items?.Length <= 0 || !dewCollector.items.Any(item => item?.count > 0))
                    {
                        continue;
                    }

                    dewCollectors.Add(dewCollector);
                    continue;
                }

                // WORKSTATION check  
                if (config.PullFromWorkstationOutputs && tileEntity is TileEntityWorkstation workstation)
                {
                    // Only player-placed workstations
                    if (!workstation.IsPlayerPlaced)
                    {
                        continue;
                    }

                    if (workstation.output?.Length <= 0 || !workstation.output.Any(item => item?.count > 0))
                    {
                        continue;
                    }

                    workstations.Add(workstation);
                    continue;
                }

                // LOOTABLE (Containers) check
                if (lootable != null)
                {
                    // Must be player storage
                    if (!lootable.bPlayerStorage)
                    {
                        continue;
                    }

                    if (config.OnlyStorageCrates && !hasStorageFeature)
                    {
                        continue;
                    }

                    if (lootable.items?.Length <= 0 || !lootable.items.Any(item => item?.count > 0))
                    {
                        continue;
                    }

                    lootables.Add(lootable);
                    continue;
                }
            }
        }

        LogUtil.DebugLog($"{d_MethodName}: Processed {chunksProcessed} chunks, {nullChunks} null chunks, {tileEntitiesProcessed} tile entities");
    }

    public static bool HasItem(BatchRemovalContext context, ItemValue itemValue)
    {
        const string d_MethodName = nameof(HasItem);

        if (itemValue == null)
        {
            LogUtil.Error($"{d_MethodName} | itemValue is null");
            return false;
        }

        if (context == null)
        {
            context = BatchRemovalContext.Create(d_MethodName);
        }

        if (context == null)
        {
            LogUtil.Error($"{d_MethodName}: Failed to create BatchRemovalContext");
            return false;
        }

        var sourceStacks = GetItemCount(context, itemValue);
        var result = sourceStacks > 0;

        LogUtil.DebugLog($"{d_MethodName} for '{itemValue?.ItemClass?.Name}' is {result}");

        return result;
    }

    public static int GetItemCount(BatchRemovalContext context, ItemValue itemValue)
    {
        const string d_MethodName = nameof(GetItemCount);

        if (itemValue == null)
        {
            LogUtil.Error($"{d_MethodName} | itemValue is null");
            return 0;
        }

        if (context == null)
        {
            context = BatchRemovalContext.Create(d_MethodName);
        }

        if (context == null)
        {
            LogUtil.Error($"{d_MethodName}: Failed to create BatchRemovalContext");
            return 0;
        }

        var sources = GetPullableSourceItemStacks(context, out var totalItemCountAdded, filterItem: itemValue, stillNeeded: -1);

        LogUtil.DebugLog($"{d_MethodName} | Found {totalItemCountAdded} of '{itemValue.ItemClass?.Name}'");

        return totalItemCountAdded;
    }

    public static int RemoveRemaining(
       ItemValue itemValue,
       int stillNeeded,
       bool ignoreModdedItems = false,
       IList<ItemStack> removedItems = null)
    {
        const string d_MethodName = nameof(RemoveRemaining);

        var context = BatchRemovalContext.Create(d_MethodName);
        if (context == null)
        {
            LogUtil.Error($"{d_MethodName}: Failed to create BatchRemovalContext");
            return 0;
        }

        int removedCount = RemoveRemainingWithContext(context, itemValue, stillNeeded, ignoreModdedItems, removedItems);

        return removedCount;
    }

    public static int RemoveRemainingWithContext(
        BatchRemovalContext context,
        ItemValue itemValue,
        int stillNeeded,
        bool ignoreModdedItems = false,
        IList<ItemStack> removedItems = null)
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
            ProcessStorage(d_MethodName, "DewCollectors", itemName, context.DewCollectors, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                dewCollector => dewCollector.items, dewCollector => DewCollectorUtils.MarkDewCollectorModified(dewCollector));
        }

        if (stillNeeded > 0 && config.PullFromWorkstationOutputs)
        {
            ProcessStorage(d_MethodName, "WorkstationOutputs", itemName, context.Workstations, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                workstation => workstation.output, workstation => WorkstationUtils.MarkWorkstationModified(workstation));
        }

        if (stillNeeded > 0)
        {
            ProcessStorage(d_MethodName, "Containers", itemName, context.Lootables, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                lootable => lootable.items, lootable => lootable.SetModified());
        }

        if (stillNeeded > 0 && config.PullFromVehicleStorage)
        {
            ProcessStorage(d_MethodName, "Vehicles", itemName, context.Vehicles, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                vehicle => vehicle.bag.items, vehicle => vehicle.SetBagModified());
        }

        return originalNeeded - stillNeeded;  // Return the total number of items removed
    }

    private static void ProcessStorage<T>(
        string d_method_name,
        string storageName,
        string itemName,
        IEnumerable<T> storages,
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