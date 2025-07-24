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


    // Helper local function to add stacks and log
    private static void AddStacks(string d_MethodName, List<ItemStack> output, IEnumerable<ItemStack> stacks, string sourceName, ref int previousCount)
    {
        if (stacks != null)
        {
            output.AddRange(stacks.Where(s => s != null));
        }

        LogUtil.DebugLog($"{d_MethodName}: {sourceName} pulled in {output.Count - previousCount} stacks");
        previousCount = output.Count;
    }

    public static List<ItemStack> GetPullableSourceItemStacks()
    {
        const string d_MethodName = "GetPullableSourceItemStacks";
        LogUtil.DebugLog($"{d_MethodName}: Starting");

        var result = new List<ItemStack>();
        int previousCount = 0;

        // Container Storage
        var containerStorage = GetAvailableContainerStorages();
        AddStacks(d_MethodName, result, containerStorage?.SelectMany(lootable => lootable?.items ?? Enumerable.Empty<ItemStack>()), "Container Storage", ref previousCount);

        // Dew Collectors
        if (ModConfig.PullFromDewCollectors())
        {
            var dewCollectors = DewCollectorUtils.GetAvailableDewCollectorStorages();
            AddStacks(d_MethodName, result, dewCollectors?.SelectMany(dewCollector => dewCollector?.items ?? Enumerable.Empty<ItemStack>()), "Dew Collector Storage", ref previousCount);
        }

        // Workstation Outputs
        if (ModConfig.PullFromWorkstationOutputs())
        {
            var workstationOutputs = WorkstationUtils.GetAvailableWorkstationOutputs();
            AddStacks(d_MethodName, result, workstationOutputs?.SelectMany(workstation => workstation?.Output ?? Enumerable.Empty<ItemStack>()), "Workstation Output", ref previousCount);
        }

        // Vehicle Storage
        if (ModConfig.PullFromVehicleStorage())
        {
            var vehicleStorage = VehicleUtils.GetAvailableVehicleStorages();
            AddStacks(d_MethodName, result, vehicleStorage?.SelectMany(vehicle => vehicle?.bag?.GetSlots() ?? Enumerable.Empty<ItemStack>()), "Vehicle Storage", ref previousCount);
        }

        return StripNullItemStacks(result);
    }

    public static bool HasItem(ItemValue itemValue)
    {
        const string d_MethodName = "HasItem";
        LogUtil.DebugLog($"{d_MethodName}: Starting");

        if (itemValue == null || itemValue.IsEmpty())
        {
            LogUtil.DebugLog($"{d_MethodName} Item is null or Empty");
            return false;
        }

        var containerStorage = GetAvailableContainerStorages();
        bool containerHas = containerStorage != null &&
            containerStorage.SelectMany(lootable => lootable?.items ?? [])
                            .Any(stack => stack?.itemValue?.type == itemValue.type);

        if (containerHas)
        {
            return true;
        }

        if (ModConfig.PullFromDewCollectors())
        {
            var dewCollectors = DewCollectorUtils.GetAvailableDewCollectorStorages();
            bool dewCollectorHas = dewCollectors != null &&
                dewCollectors.SelectMany(dewCollector => dewCollector?.items ?? [])
                             .Any(stack => stack?.itemValue?.type == itemValue.type);

            if (dewCollectorHas)
            {
                return true;
            }
        }

        if (ModConfig.PullFromWorkstationOutputs())
        {
            var workstationOutputs = WorkstationUtils.GetAvailableWorkstationOutputs();
            bool workstationHas = workstationOutputs != null &&
                workstationOutputs.SelectMany(workstation => workstation?.Output ?? [])
                                  .Any(stack => stack?.itemValue?.type == itemValue.type);

            if (workstationHas)
            {
                return true;
            }
        }

        if (ModConfig.PullFromVehicleStorage())
        {
            var vehicleStorage = VehicleUtils.GetAvailableVehicleStorages();
            bool vehicleHas = vehicleStorage != null &&
                vehicleStorage.SelectMany(vehicle => vehicle?.bag?.items ?? [])
                              .Any(stack => stack?.itemValue?.type == itemValue.type);

            if (vehicleHas)
            {
                return true;
            }
        }

        return false;
    }

    public static int GetItemCount(ItemValue itemValue)
    {
        const string d_MethodName = "GetItemCount";
        LogUtil.DebugLog($"{d_MethodName}: Starting");

        if (itemValue == null || itemValue.IsEmpty())
        {
            LogUtil.DebugLog($"{d_MethodName} Item is null or Empty");
            return 0;
        }

        static int CountItems(IEnumerable<ItemStack> stacks, int type)
            => stacks?.Where(stack => stack?.itemValue?.type == type).Sum(stack => stack?.count) ?? 0;

        var containerStorage = GetAvailableContainerStorages();
        int containerCount = CountItems(containerStorage?.SelectMany(lootable => lootable?.items), itemValue.type);
        LogUtil.DebugLog($"{d_MethodName} Container Storage count is {containerCount}");
        int totalCount = containerCount;

        if (ModConfig.PullFromDewCollectors())
        {
            LogUtil.DebugLog($"{d_MethodName} Will try to pull from Dew Collectors");

            var dewCollectors = DewCollectorUtils.GetAvailableDewCollectorStorages();
            int dewCollectorCount = CountItems(dewCollectors?.SelectMany(dewCollector => dewCollector?.items ?? []), itemValue.type);
            LogUtil.DebugLog($"{d_MethodName} Dew Collector count is {dewCollectorCount}");

            totalCount += dewCollectorCount;
        }

        if (ModConfig.PullFromWorkstationOutputs())
        {
            LogUtil.DebugLog($"{d_MethodName} Will try to pull from Workstation Outputs");

            var workstationOutputs = WorkstationUtils.GetAvailableWorkstationOutputs();
            int workstationOutputsCount = CountItems(workstationOutputs?.SelectMany(workstation => workstation?.Output ?? []), itemValue.type);
            LogUtil.DebugLog($"{d_MethodName} Workstation Output count is {workstationOutputsCount}");

            totalCount += workstationOutputsCount;
        }

        if (ModConfig.PullFromVehicleStorage())
        {
            LogUtil.DebugLog($"{d_MethodName} Will try to pull from Vehicle Storage");

            var vehicleStorage = VehicleUtils.GetAvailableVehicleStorages();
            int vehicleCount = CountItems(vehicleStorage?.SelectMany(vehicle => vehicle?.bag?.items ?? []), itemValue.type);
            LogUtil.DebugLog($"{d_MethodName} Vehicle Storage count is {vehicleCount}");

            totalCount += vehicleCount;
        }

        return totalCount;
    }

    private static List<ITileEntityLootable> GetAvailableContainerStorages()
    {
        const string d_MethodName = "GetAvailableContainerStorages";

        var world = GameManager.Instance.World;
        if (world == null)
        {
            LogUtil.DebugLog($"{d_MethodName}: World is null, aborting.");
            return new List<ITileEntityLootable>();
        }

        var player = world.GetPrimaryPlayer();
        if (player == null)
        {
            LogUtil.DebugLog($"{d_MethodName}: Player is null, aborting.");
            return new List<ITileEntityLootable>();
        }

        LogUtil.DebugLog($"{d_MethodName}: Starting");

        var playerPos = player.position;
        var configRange = ModConfig.Range();
        var configOnlyCrates = ModConfig.OnlyStorageCrates();
        var internalLocalUserIdentifier = PlatformManager.InternalLocalUserIdentifier;
        var playerEntityId = player.entityId;

        var chunkCacheCopy = world.ChunkCache.GetChunkArrayCopySync();
        if (chunkCacheCopy == null)
        {
            LogUtil.DebugLog($"{d_MethodName}: chunkCacheCopy is null, aborting.");
            return new List<ITileEntityLootable>();
        }

        var result = new List<ITileEntityLootable>();

        foreach (var chunk in chunkCacheCopy)
        {
            if (chunk == null)
            {
                continue;
            }

            foreach (var tileEntity in chunk.GetTileEntities().list)
            {
                // Range check first for early exit
                if (configRange > 0 && Vector3.Distance(playerPos, tileEntity.ToWorldPos()) >= configRange)
                {
                    continue;
                }

                // Must be lootable
                if (!tileEntity.TryGetSelfOrFeature(out ITileEntityLootable tileEntityLootable))
                {
                    continue;
                }

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

                // Locked check (skip if locked by another player)
                if (LockedTileEntities.Count > 0)
                {
                    var pos = tileEntityLootable.ToWorldPos();
                    if (LockedTileEntities.TryGetValue(pos, out int entityId) && entityId != playerEntityId)
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

                result.Add(tileEntityLootable);
            }
        }

        return result;
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

            if (stack == null)
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
        const string d_MethodName = "RemoveRemaining";

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

        if (stillNeeded > 0)
        {
            var containerStorages = GetAvailableContainerStorages() ?? [];
            ProcessStorage(d_MethodName, "Containers", itemName, containerStorages, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                s => s.items, s => s.SetModified());
        }

        if (stillNeeded > 0 && ModConfig.PullFromDewCollectors())
        {
            var dewCollectors = DewCollectorUtils.GetAvailableDewCollectorStorages() ?? [];
            ProcessStorage(d_MethodName, "DewCollectors", itemName, dewCollectors, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                s => s.items, s => DewCollectorUtils.MarkDewCollectorModified(s));
        }

        if (stillNeeded > 0 && ModConfig.PullFromWorkstationOutputs())
        {
            var workstationOutputs = WorkstationUtils.GetAvailableWorkstationOutputs() ?? [];
            ProcessStorage(d_MethodName, "WorkstationOutputs", itemName, workstationOutputs, itemValue, ref stillNeeded, ignoreModdedItems, removedItems,
                s => s.Output, s => WorkstationUtils.MarkWorkstationModified(s));
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

    public static List<ItemStack> StripNullItemStacks(IEnumerable<ItemStack> input)
    {
        // This is because of the brain dead game code that adds null item stacks to the list

        var result = new List<ItemStack>();
        if (input == null)
        {
            return result;
        }

        foreach (var stack in input)
        {
            if (stack == null)
            {
                continue;
            }

            if (stack.count <= 0)
            {
                continue;
            }

            if (stack.itemValue == null || stack.itemValue.IsEmpty())
            {
                continue;
            }

            var itemClass = stack.itemValue.ItemClass;
            if (itemClass == null)
            {
                continue;
            }

            var itemName = itemClass.GetItemName();
            if (string.IsNullOrEmpty(itemName))
            {
                continue;
            }

            result.Add(stack);
        }
        return result;
    }
}