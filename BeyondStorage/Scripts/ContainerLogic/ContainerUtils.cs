using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Server;
using BeyondStorage.Scripts.Utils;
using Platform;
using UnityEngine;


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

    public static List<ItemStack> GetPullableSourceItemStacks(out int totalItemsAddedCount, ItemValue filterItem = null, int stillNeeded = -1)
    {
        const string d_MethodName = nameof(GetPullableSourceItemStacks);

        totalItemsAddedCount = 0;

        if (stillNeeded == 0)
        {
            return [];
        }

        var context = new BatchRemovalContext();
        var config = context.Config;

        var result = new List<ItemStack>(ItemUtil.DEFAULT_ITEMSTACK_LIST_CAPACITY);

        LogUtil.DebugLog($"{d_MethodName}: Found {context.DewCollectors.Count} dew collectors, {context.Workstations.Count} workstations, {context.Lootables.Count} lootables, {context.Vehicles.Count} vehicles, stillNeeded {stillNeeded}");

        if (config.PullFromDewCollectors)
        {
            AddValidItemStacksFromSources(d_MethodName, result, context.DewCollectors, dc => dc.items,
                "Dew Collector Storage", out int dewCollectorItemsAddedCount, ref stillNeeded, filterItem);
            //LogUtil.DebugLog($"{d_MethodName}: Found {context.DewCollectors.Count} dew collectors, added {dewCollectorItemsAddedCount} items, stillNeeded {stillNeeded}");

            totalItemsAddedCount += dewCollectorItemsAddedCount;
            if (stillNeeded == 0)
            {
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
                return result;
            }
        }

        if (config.PullFromVehicleStorage)
        {
            AddValidItemStacksFromSources(d_MethodName, result, context.Vehicles, v => v.bag?.GetSlots(),
                "Vehicle Storage", out int vehicleItemsAddedCount, ref stillNeeded, filterItem);
            //LogUtil.DebugLog($"{d_MethodName}: Found {context.Vehicles.Count} vehicles, added {vehicleItemsAddedCount} items, stillNeeded {stillNeeded}");

            totalItemsAddedCount += vehicleItemsAddedCount;
            if (stillNeeded == 0)
            {
                return result;
            }
        }

        return result;
    }

    public static void DiscoverTileEntitySources(
        ConfigSnapshot config,
        out List<TileEntityDewCollector> dewCollectors,
        out List<TileEntityWorkstation> workstations,
        out List<ITileEntityLootable> lootables)
    {
        dewCollectors = new List<TileEntityDewCollector>(DEFAULT_DEW_COLLECTOR_LIST_CAPACITY);
        workstations = new List<TileEntityWorkstation>(DEFAULT_WORKSTATION_LIST_CAPACITY);
        lootables = new List<ITileEntityLootable>(DEFAULT_LOOTBLE_LIST_CAPACITY);

        AddPullableTileEntities(config, dewCollectors, workstations, lootables);
    }

    private static void AddPullableTileEntities(
        ConfigSnapshot config,
        List<TileEntityDewCollector> dewCollectors,
        List<TileEntityWorkstation> workstations,
        List<ITileEntityLootable> lootables)
    {
        const string d_MethodName = nameof(AddPullableTileEntities);

        var world = GameManager.Instance.World;
        if (world == null)
        {
            LogUtil.Error($"{d_MethodName}: World is null, aborting.");
            return;
        }

        var player = world.GetPrimaryPlayer();
        if (player == null)
        {
            LogUtil.Error($"{d_MethodName}: Player is null, aborting.");
            return;
        }

        var chunkCacheCopy = world.ChunkCache.GetChunkArrayCopySync();
        if (chunkCacheCopy == null)
        {
            LogUtil.Error($"{d_MethodName}: chunkCacheCopy is null, aborting.");
            return;
        }

        var playerPos = player.position;
        var internalLocalUserIdentifier = PlatformManager.InternalLocalUserIdentifier;
        var playerEntityId = player.entityId;

        int chunksProcessed = 0;
        int nullChunks = 0;
        int tileEntitiesProcessed = 0;

        foreach (var chunk in chunkCacheCopy)
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
                    if (LockedTileEntities.TryGetValue(tileEntityWorldPos, out int entityId) && entityId != playerEntityId)
                    {
                        continue;
                    }
                }

                // Range check first for early exit
                if (config.Range > 0 && Vector3.Distance(playerPos, tileEntityWorldPos) >= config.Range)
                {
                    continue;
                }

                // Lockable check (skip if locked and not allowed)
                if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
                {
                    if (tileLockable.IsLocked() && !tileLockable.IsUserAllowed(internalLocalUserIdentifier))
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

    public static bool HasItem(ItemValue itemValue)
    {
        const string d_MethodName = nameof(HasItem);

        if (itemValue == null)
        {
            LogUtil.Error($"{d_MethodName} | itemValue is null");
            return false;
        }

        var sourceStacks = GetItemCount(itemValue, stillNeeded: 1);
        var result = sourceStacks > 0;

        LogUtil.DebugLog($"{d_MethodName} for '{itemValue?.ItemClass?.Name}' is {result}");

        return result;
    }

    public static int GetItemCount(ItemValue itemValue, int stillNeeded = -1)
    {
        const string d_MethodName = nameof(GetItemCount);

        if (itemValue == null)
        {
            LogUtil.Error($"{d_MethodName} | itemValue is null");
            return 0;
        }

        var sources = GetPullableSourceItemStacks(out var totalItemCountAdded, filterItem: itemValue, stillNeeded: stillNeeded);

        LogUtil.DebugLog($"{d_MethodName} | Found {totalItemCountAdded} of '{itemValue.ItemClass?.Name}'");

        return totalItemCountAdded;
    }

    public static int RemoveRemaining(
       ItemValue itemValue,
       int stillNeeded,
       bool ignoreModdedItems = false,
       IList<ItemStack> removedItems = null)
    {
        var context = new BatchRemovalContext();
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