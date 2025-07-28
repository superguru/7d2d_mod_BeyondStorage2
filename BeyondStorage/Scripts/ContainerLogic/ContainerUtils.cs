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
    public const int DEFAULT_ITEMSTACK_LIST_CAPACITY = 1024; // 10 container with 100 slots each, rounded up
    public const int DEFAULT_DEW_COLLECTOR_LIST_CAPACITY = 16; // 10 dew collectors, rounded up

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

    private static void InitializePullableCollections(out List<ITileEntityLootable> containerStorages, out List<TileEntityDewCollector> dewCollectors)
    {
        // Used to initialize the collections for pullable tile entities where you have to enumerate all the tile entities in the world.

        containerStorages = new List<ITileEntityLootable>(DEFAULT_ITEMSTACK_LIST_CAPACITY);
        dewCollectors = new List<TileEntityDewCollector>(DEFAULT_DEW_COLLECTOR_LIST_CAPACITY);

        AddPullableTileEntities(containerStorages, dewCollectors);
    }

    private static void AddPullableTileEntities(List<ITileEntityLootable> lootables, List<TileEntityDewCollector> dewCollectors)
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

        foreach (var chunk in chunkCacheCopy)
        {
            if (chunk == null)
            {
                continue;
            }

            foreach (var tileEntity in chunk.GetTileEntities().list)
            {
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

                    // Lockable check (skip if locked and not allowed)
                    if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
                    {
                        if (tileLockable.IsLocked() && !tileLockable.IsUserAllowed(internalLocalUserIdentifier))
                        {
                            continue;
                        }
                    }

                    lootables.Add(tileEntityLootable);
                    continue;
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
                }
            }

        }
    }

    private static void AddStacks(string d_MethodName, List<ItemStack> output, IEnumerable<ItemStack> stacks, string sourceName, ref int previousCount)
    {
        if (stacks != null)
        {
            output.AddRange(stacks.Where(stack => stack != null && stack.count > 0));
        }

        LogUtil.DebugLog($"{d_MethodName}: {sourceName} pulled in {output.Count - previousCount} stacks");
        previousCount = output.Count;
    }

    public static List<ItemStack> GetPullableSourceItemStacks()
    {
        const string d_MethodName = nameof(GetPullableSourceItemStacks);
        LogUtil.DebugLog($"{d_MethodName}: Starting");

        var result = new List<ItemStack>(DEFAULT_ITEMSTACK_LIST_CAPACITY);
        int previousCount = 0;

        InitializePullableCollections(out var containerStorages, out var dewCollectors);

        // Dew Collectors
        AddStacks(d_MethodName, result, dewCollectors.SelectMany(dewCollector => dewCollector?.items ?? Enumerable.Empty<ItemStack>()), "Dew Collector Storage", ref previousCount);

        // Workstation Outputs
        if (ModConfig.PullFromWorkstationOutputs())
        {
            var workstationOutputs = WorkstationUtils.GetAvailableWorkstationOutputs();
            AddStacks(d_MethodName, result, workstationOutputs?.SelectMany(workstation => workstation?.Output ?? Enumerable.Empty<ItemStack>()), "Workstation Output", ref previousCount);
        }

        // Container Storage
        AddStacks(d_MethodName, result, containerStorages.SelectMany(lootable => lootable.items ?? Enumerable.Empty<ItemStack>()), "Container Storage", ref previousCount);

        // Vehicle Storage
        if (ModConfig.PullFromVehicleStorage())
        {
            var vehicleStorage = VehicleUtils.GetAvailableVehicleStorages();
            AddStacks(d_MethodName, result, vehicleStorage?.SelectMany(vehicle => vehicle?.bag?.GetSlots() ?? Enumerable.Empty<ItemStack>()), "Vehicle Storage", ref previousCount);
        }

        return StripNullAndEmptyItemStacks(result);
    }

    public static bool HasItem(ItemValue itemValue)
    {
        const string d_MethodName = nameof(HasItem);

        if (itemValue == null || itemValue.IsEmpty())
        {
            LogUtil.Error($"{d_MethodName} Item is null or Empty");
            return false;
        }

        var itemClass = itemValue.ItemClass;
        if (itemClass == null || string.IsNullOrEmpty(itemClass.Name))
        {
            LogUtil.Error($"{d_MethodName} ItemClass is null for item type {itemValue.type}");
            return false;
        }

        var sources = GetPullableSourceItemStacks();
        var result = sources.Any(stack => stack.itemValue.type == itemValue.type);

        LogUtil.DebugLog($"{d_MethodName} for {itemClass.Name} is {result}");

        return result;
    }

    public static int GetItemCount(ItemValue itemValue)
    {
        const string d_MethodName = nameof(GetItemCount);

        if (itemValue == null || itemValue.IsEmpty())
        {
            LogUtil.Error($"{d_MethodName} Item is null or Empty");
            return 0;
        }

        var itemClass = itemValue.ItemClass;
        if (itemClass == null || string.IsNullOrEmpty(itemClass.Name))
        {
            LogUtil.Error($"{d_MethodName} ItemClass is null for item type {itemValue.type}");
            return 0;
        }

        var sources = GetPullableSourceItemStacks();
        var totalCount = sources.Where(stack => stack?.itemValue.type == itemValue.type).Sum(stack => stack?.count) ?? 0;

        LogUtil.DebugLog($"{d_MethodName} | Found {totalCount} of {itemClass.Name}");

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

        InitializePullableCollections(out var containerStorages, out var dewCollectors);

        if (stillNeeded > 0 && ModConfig.PullFromDewCollectors())
        {
            ProcessStorage(d_MethodName, "DewCollectors", itemName, dewCollectors, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                s => s.items, s => DewCollectorUtils.MarkDewCollectorModified(s));
        }

        if (stillNeeded > 0 && ModConfig.PullFromWorkstationOutputs())
        {
            var workstationOutputs = WorkstationUtils.GetAvailableWorkstationOutputs() ?? [];
            ProcessStorage(d_MethodName, "WorkstationOutputs", itemName, workstationOutputs, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                s => s.Output, s => WorkstationUtils.MarkWorkstationModified(s));
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

    /// <summary>
    /// Initializes empty collections for pullable container storages and dew collectors,
    /// then populates them with available tile entities that meet the configured criteria.
    /// Will strip out null and empty item stacks from the input collection.
    /// </summary>
    /// <remarks>
    /// This method serves as a convenience wrapper that:
    /// <list type="bullet">
    /// <item><description>Creates empty collections for both storage types</description></item>
    /// <item><description>Calls <see cref="AddPullableTileEntities"/> to populate them</description></item>
    /// <item><description>Applies all configured filters (range, locking, storage type restrictions)</description></item>
    /// </list>
    /// The populated collections respect all mod configuration settings including:
    /// range limits, storage crate restrictions, dew collector inclusion, and player permissions.
    /// </remarks>
    /// <summary>
    /// Efficiently removes null and empty item stacks from the input collection.
    /// Optimized for performance with minimal allocations and early exits.
    /// </summary>
    /// <param name="input">The collection of item stacks to filter</param>
    /// <returns>A new list containing only valid, non-empty item stacks</returns>
    public static List<ItemStack> StripNullAndEmptyItemStacks(IEnumerable<ItemStack> input)
    {
        // Early exit for null input
        if (input == null)
        {
            return [];
        }

        // If input is already a collection, we can pre-allocate with capacity
        var result = input is ICollection<ItemStack> collection
            ? new List<ItemStack>(Math.Min(collection.Count, DEFAULT_ITEMSTACK_LIST_CAPACITY)) // Cap initial capacity to avoid over-allocation
            : new List<ItemStack>(DEFAULT_ITEMSTACK_LIST_CAPACITY);

        foreach (var stack in input)
        {
            // Combined null and count check for early exit
            if (stack?.count > 0)
            {
                var itemValue = stack.itemValue;
                // Combined null and empty check
                if (itemValue?.ItemClass != null && !itemValue.IsEmpty())
                {
                    // Only check item name if we've passed all other checks
                    // Most items will have valid names, so this is the least likely to fail
                    var itemName = itemValue.ItemClass.GetItemName();
                    if (!string.IsNullOrEmpty(itemName))
                    {
                        result.Add(stack);
                    }
                }
            }
        }

        return result;
    }
}