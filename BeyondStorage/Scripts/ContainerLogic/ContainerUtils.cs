using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BeyondStorage.Scripts.Configuration;
using BeyondStorage.Scripts.Server;
using BeyondStorage.Scripts.Utils;
using Platform;
using UnityEngine;


namespace BeyondStorage.Scripts.ContainerLogic;

public static class ContainerUtils
{
    public const int DEFAULT_DEW_COLLECTOR_LIST_CAPACITY = 16; // 10 dew collectors, rounded up
    public const int DEFAULT_ITEMSTACK_LIST_CAPACITY = 1024;   // 10 container with 100 slots each, rounded up
    public const int DEFAULT_WORKSTATION_LIST_CAPACITY = 32;   // this is all you'll ever need (tm)

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

    private static void InitializePullableCollections(
        out List<ITileEntityLootable> lootables,
        out List<TileEntityDewCollector> dewCollectors,
        out List<TileEntityWorkstation> workstations)
    {
        // Used to initialize the collections for pullable tile entities where you have to enumerate all the tile entities in the world.

        lootables = new List<ITileEntityLootable>(DEFAULT_ITEMSTACK_LIST_CAPACITY);
        dewCollectors = new List<TileEntityDewCollector>(DEFAULT_DEW_COLLECTOR_LIST_CAPACITY);
        workstations = new List<TileEntityWorkstation>(DEFAULT_WORKSTATION_LIST_CAPACITY);

        AddPullableTileEntities(lootables, dewCollectors, workstations);
    }

    private static void AddPullableTileEntities(
        List<ITileEntityLootable> lootables,
        List<TileEntityDewCollector> dewCollectors,
        List<TileEntityWorkstation> workstations)
    {
        const string d_MethodName = nameof(AddPullableTileEntities);

        var world = GameManager.Instance.World;
        if (world == null)
        {
            LogUtil.Error($"{d_MethodName}: World is null, aborting.");
        }

        var player = world.GetPrimaryPlayer();
        if (player == null)
        {
            LogUtil.Error($"{d_MethodName}: Player is null, aborting.");
        }
        var chunkCacheCopy = world.ChunkCache.GetChunkArrayCopySync();
        if (chunkCacheCopy == null)
        {
            LogUtil.Error($"{d_MethodName}: chunkCacheCopy is null, aborting.");
        }

        LogUtil.DebugLog($"{d_MethodName}: Starting");

        var playerPos = player.position;
        var configRange = ModConfig.Range();
        var internalLocalUserIdentifier = PlatformManager.InternalLocalUserIdentifier;
        var playerEntityId = player.entityId;

        // Selectors
        var configOnlyCrates = ModConfig.OnlyStorageCrates();
        var includeDewCollectors = ModConfig.PullFromDewCollectors();
        var includeWorkstationOutputs = ModConfig.PullFromWorkstationOutputs();

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

            foreach (var tileEntity in chunk.GetTileEntities().list)
            {
                tileEntitiesProcessed++;

                var worldPos = tileEntity.ToWorldPos();

                // Range check first for early exit
                if (configRange > 0 && Vector3.Distance(playerPos, worldPos) >= configRange)
                {
                    continue;
                }

                // Locked check (skip if locked by another player)
                if (LockedTileEntities.Count > 0)
                {
                    if (LockedTileEntities.TryGetValue(worldPos, out int entityId) && entityId != playerEntityId)
                    {
                        continue;
                    }
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
                if (includeDewCollectors && tileEntity is TileEntityDewCollector dewCollector)
                {
                    // Consider a dew collector empty if all items are empty or null. This might not be needed, and could possible be removed for improveed performance.
                    bool isEmpty = dewCollector.items.All(item => item?.IsEmpty() ?? true);
                    if (isEmpty)
                    {
                        continue;
                    }

                    dewCollectors.Add(dewCollector);
                    continue;
                }

                // WORKSTATION check
                if (includeWorkstationOutputs && tileEntity is TileEntityWorkstation workstation)
                {
                    // Only player-placed workstations
                    if (!workstation.IsPlayerPlaced)
                    {
                        continue;
                    }

                    // Must have output and not be empty
                    if (workstation.output == null || !workstation.output.Any(stack => stack?.count > 0))
                    {
                        continue;
                    }

                    if (workstation.OutputEmpty())
                    {
                        continue;
                    }

                    // Skip if being removed
                    if (workstation.IsRemoving)
                    {
                        continue;
                    }

                    LogUtil.DebugLog($"{d_MethodName}: Found workstation {workstation.GetType().Name} at {worldPos}, chunk is at {chunk.ChunkPos}");
                    workstations.Add(workstation);
                    continue;
                }

                // LOOTABLE check
                if (tileEntity.TryGetSelfOrFeature(out ITileEntityLootable tileEntityLootable))
                {

                    // Must be player storage and not empty
                    if (!tileEntityLootable.bPlayerStorage || tileEntityLootable.IsEmpty())
                    {
                        continue;
                    }

                    // Only crates if configured
                    if (configOnlyCrates && !tileEntity.TryGetSelfOrFeature(out TEFeatureStorage _))
                    {
                        continue;
                    }

                    lootables.Add(tileEntityLootable);
                    continue;
                }
            }
        }

        LogUtil.DebugLog($"{d_MethodName}: Processed {chunksProcessed} chunks, {nullChunks} null chunks, {tileEntitiesProcessed} tile entities");
    }

    private static void AddValidStacks<T>(string d_MethodName,
        List<ItemStack> output,
        IEnumerable<T> sources,
        Func<T, ItemStack[]> getStacks,
        string sourceName,
        ref int previousCount,
        ItemValue filterItem = null) where T : class
    {
        if (sources == null)
        {
            LogUtil.DebugLog($"{d_MethodName}: {sourceName} pulled in 0 stacks (null source)");
            return;
        }

        int addedCount = 0;
        int filterType = filterItem?.type ?? -1;

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
                if (stack == null || stack.count <= 0)
                {
                    continue;
                }

                // Apply filter if specified
                if (filterType >= 0 && stack.itemValue?.type != filterType)
                {
                    continue;
                }

                output.Add(stack);
                addedCount++;
            }
        }

        LogUtil.DebugLog($"{d_MethodName}: {sourceName} pulled in {addedCount} stacks");
        previousCount = output.Count;
    }

    public static List<ItemStack> GetPullableSourceItemStacks(ItemValue filterItem = null)
    {
        const string d_MethodName = nameof(GetPullableSourceItemStacks);
        LogUtil.DebugLog($"{d_MethodName}: Starting");

        var result = new List<ItemStack>(ItemUtil.DEFAULT_ITEMSTACK_LIST_CAPACITY);
        int previousCount = 0;

        InitializePullableCollections(out var lootables, out var dewCollectors, out var workstations);

        AddValidStacks(d_MethodName, result, dewCollectors, dc => dc.items, "Dew Collector Storage", ref previousCount, filterItem);

        if (ModConfig.PullFromWorkstationOutputs())
        {
            AddValidStacks(d_MethodName, result, workstations, ws => ws.Output, "Workstation Output", ref previousCount, filterItem);
        }

        AddValidStacks(d_MethodName, result, lootables, l => l.items, "Container Storage", ref previousCount, filterItem);

        if (ModConfig.PullFromVehicleStorage())
        {
            var vehicleStorages = VehicleUtils.GetAvailableVehicleStorages();
            AddValidStacks(d_MethodName, result, vehicleStorages, v => v.bag?.GetSlots(), "Vehicle Storage", ref previousCount, filterItem);
        }

        ItemUtil.PurgeInvalidItemStacks(result);
        return result;
    }

    public static bool HasItem(ItemValue itemValue)
    {
        const string d_MethodName = nameof(HasItem);

        var sources = GetPullableSourceItemStacks(itemValue);
        var result = sources.Any();

        LogUtil.DebugLog($"{d_MethodName} for '{itemValue?.ItemClass?.Name}' is {result}");

        return result;
    }

    public static int GetItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(GetItemCount);

        var sources = GetPullableSourceItemStacks(itemValue);
        var totalCount = sources.Sum(stack => stack.count);

        LogUtil.DebugLog($"{d_MethodName} | Found {totalCount} of '{itemValue?.ItemClass?.Name}'");

        return totalCount;
    }

    private static int RemoveItems(ItemStack[] items, ItemValue desiredItem, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        if (desiredItem == null || desiredItem.IsEmpty() || stillNeeded <= 0)
        {
            return stillNeeded;
        }

        foreach (var stack in items)
        {
            if (stillNeeded <= 0)
            {
                break;
            }

            if (stack == null || stack.count <= 0)
            {
                continue;
            }

            var itemValue = stack.itemValue;
            if (itemValue == null || itemValue.type != desiredItem.type)
            {
                continue;
            }

            if (ignoreModdedItems && itemValue.HasModSlots && itemValue.HasMods())
            {
                continue;
            }

            if (ItemClass.GetForId(itemValue.type).CanStack())
            {
                var countToRemove = Math.Min(stack.count, stillNeeded);
#if DEBUG
                LogUtil.DebugLog($"RemoveItems Item Count Before: {stack.count} Count To Remove: {countToRemove}");
#endif
                removedItems?.Add(new ItemStack(itemValue.Clone(), countToRemove));
                stack.count -= countToRemove;
                stillNeeded -= countToRemove;
#if DEBUG
                LogUtil.DebugLog($"RemoveItems Item Count After: {stack.count} Count Still Required {stillNeeded}");
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

    public static int RemoveRemaining(ItemValue itemValue, int stillNeeded, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        const string d_MethodName = nameof(RemoveRemaining);

        if (stillNeeded <= 0 || itemValue == null || itemValue.ItemClass == null || itemValue.IsEmpty())
        {
#if DEBUG
            LogUtil.DebugLog($"{d_MethodName} | Null or empty item; stillNeeded {stillNeeded}; null is {itemValue == null}");
#endif
            return 0;
        }

        var itemName = itemValue.ItemClass.GetItemName();
        LogUtil.DebugLog($"{d_MethodName} | Trying to remove {stillNeeded} {itemName}");

        int originalNeeded = stillNeeded;

        InitializePullableCollections(out var containerStorages, out var dewCollectors, out var workstations);

        if (stillNeeded > 0 && ModConfig.PullFromDewCollectors())
        {
            ProcessStorage(d_MethodName, "DewCollectors", itemName, dewCollectors, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                dewCollector => dewCollector.items, s => DewCollectorUtils.MarkDewCollectorModified(s));
        }

        if (stillNeeded > 0 && ModConfig.PullFromWorkstationOutputs())
        {
            ProcessStorage(d_MethodName, "WorkstationOutputs", itemName, workstations, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                workstation => workstation.Output, s => WorkstationUtils.MarkWorkstationModified(s));
        }

        if (stillNeeded > 0)
        {
            ProcessStorage(d_MethodName, "Containers", itemName, containerStorages, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                s => s.items, s => s.SetModified());
        }

        if (stillNeeded > 0 && ModConfig.PullFromVehicleStorage())
        {
            var vehicleStorages = VehicleUtils.GetAvailableVehicleStorages() ?? [];
            ProcessStorage(d_MethodName, "Vehicles", itemName, vehicleStorages, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                s => s.bag.items, s => s.SetBagModified());
        }

        return originalNeeded - stillNeeded;
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
        Func<T, ItemStack[]> getItems,
        Action<T> markModified)
    {
        int before = stillNeeded;
        foreach (var storage in storages)
        {
            if (stillNeeded <= 0)
            {
                break;
            }

            int newNeeded = RemoveItems(getItems(storage), itemValue, stillNeeded, ignoreModdedItems, removedItems);
            if (stillNeeded != newNeeded)
            {
                markModified(storage);
            }
            stillNeeded = newNeeded;
        }
        int removed = before - stillNeeded;
        LogUtil.DebugLog($"{d_method_name} | {storageName} | Removed {removed} {itemName}, stillNeeded {stillNeeded}");

#if DEBUG
        if (stillNeeded < 0)
        {
            LogUtil.Error($"{d_method_name} | stillNeeded after {storageName} should not be negative, but is {stillNeeded}");
            stillNeeded = 0;
        }
#endif
    }
}