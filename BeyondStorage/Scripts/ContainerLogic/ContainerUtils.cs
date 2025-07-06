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
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog($"UpdateLockedTEs: newCount {lockedTileEntities.Count}");
        }
    }

    public static IEnumerable<ItemStack> GetItemStacks()
    {
        var containerStorage = GetAvailableStorages();
        var containerResults = containerStorage?.SelectMany(lootable => lootable.items) ?? Enumerable.Empty<ItemStack>();

        if (!ModConfig.PullFromVehicleStorage())
        {
            return containerResults;
        }

        var vehicleStorage = VehicleUtils.GetAvailableVehicleStorages();
        var vehicleResults = vehicleStorage?.SelectMany(vehicle => vehicle.bag?.GetSlots() ?? Enumerable.Empty<ItemStack>()) ?? Enumerable.Empty<ItemStack>();

        return containerResults.Concat(vehicleResults);
    }

    public static bool HasItem(ItemValue itemValue)
    {
        var containerStorage = GetAvailableStorages();
        bool containerHas = containerStorage != null &&
            containerStorage.SelectMany(lootable => lootable.items)
                            .Any(stack => stack.itemValue.type == itemValue.type);

        if (containerHas)
        {
            return true;
        }

        if (!ModConfig.PullFromVehicleStorage())
        {
            return false;
        }

        var vehicleStorage = VehicleUtils.GetAvailableVehicleStorages();
        return vehicleStorage != null &&
            vehicleStorage.SelectMany(vehicle => vehicle.bag?.items ?? Enumerable.Empty<ItemStack>())
                          .Any(stack => stack.itemValue.type == itemValue.type);
    }

    public static int GetItemCount(ItemValue itemValue)
    {
        static int CountItems(IEnumerable<ItemStack> stacks, int type)
            => stacks?.Where(stack => stack.itemValue.type == type).Sum(stack => stack.count) ?? 0;

        var containerStorage = GetAvailableStorages();
        int containerCount = CountItems(containerStorage?.SelectMany(lootable => lootable.items), itemValue.type);

        if (!ModConfig.PullFromVehicleStorage())
        {
            return containerCount;
        }

        var vehicleStorage = VehicleUtils.GetAvailableVehicleStorages();
        int vehicleCount = CountItems(vehicleStorage?.SelectMany(vehicle => vehicle.bag?.items ?? Enumerable.Empty<ItemStack>()), itemValue.type);

        return containerCount + vehicleCount;
    }

    private static IEnumerable<ITileEntityLootable> GetAvailableStorages()
    {
        var player = GameManager.Instance.World.GetPrimaryPlayer();
        var playerPos = player.position;
        var configRange = ModConfig.Range();
        var configOnlyCrates = ModConfig.OnlyStorageCrates();
        var internalLocalUserIdentifier = PlatformManager.InternalLocalUserIdentifier;

        var chunkCacheCopy = GameManager.Instance.World.ChunkCache.GetChunkArrayCopySync();

        foreach (var tileEntity in chunkCacheCopy.Where(chunk => chunk != null).SelectMany(chunk => chunk.GetTileEntities().list))
        {
            if (!tileEntity.TryGetSelfOrFeature(out ITileEntityLootable tileEntityLootable))
            {
                continue;
            }

            if (!tileEntityLootable.bPlayerStorage || tileEntityLootable.IsEmpty())
            {
                continue;
            }

            if (configOnlyCrates && !tileEntity.TryGetSelfOrFeature(out TEFeatureStorage _))
            {
                continue;
            }

#if DEBUG
            LogUtil.DebugLog($"TEL: {tileEntityLootable}; Locked Count: {LockedTileEntities.Count}; {tileEntity.IsUserAccessing()}");
#endif

            if (LockedTileEntities.Count > 0)
            {
                var pos = tileEntityLootable.ToWorldPos();
                if (LockedTileEntities.TryGetValue(pos, out int entityId) && entityId != player.entityId)
                {
                    continue;
                }
            }

            if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
            {
                if (tileLockable.IsLocked() && !tileLockable.IsUserAllowed(internalLocalUserIdentifier))
                {
                    continue;
                }
            }

            if (configRange <= 0 || Vector3.Distance(playerPos, tileEntity.ToWorldPos()) < configRange)
            {
                yield return tileEntityLootable;
            }
        }
    }

    private static int RemoveItems(ItemStack[] items, ItemValue desiredItem, int requiredAmount, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        for (var index = 0; requiredAmount > 0 && index < items.Length; ++index)
        {
            var stack = items[index];
            if (stack.itemValue.type != desiredItem.type ||
                (ignoreModdedItems && stack.itemValue.HasModSlots && stack.itemValue.HasMods()))
            {
                continue;
            }

            if (ItemClass.GetForId(stack.itemValue.type).CanStack())
            {
                var countToRemove = Mathf.Min(stack.count, requiredAmount);
#if DEBUG
                if (LogUtil.IsDebug())
                {
                    LogUtil.DebugLog($"Item Count: {stack.count} Count To Remove: {countToRemove}");
                    LogUtil.DebugLog($"Item Count Before: {stack.count}");
                }
#endif
                removedItems?.Add(new ItemStack(stack.itemValue.Clone(), countToRemove));
                stack.count -= countToRemove;
                requiredAmount -= countToRemove;
#if DEBUG
                if (LogUtil.IsDebug())
                {
                    LogUtil.DebugLog($"Item Count After: {stack.count}");
                    LogUtil.DebugLog($"Required After: {requiredAmount}");
                }
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
                --requiredAmount;
            }
        }

        return requiredAmount;
    }

    public static int RemoveRemaining(ItemValue itemValue, int requiredAmount, bool ignoreModdedItems = false, IList<ItemStack> removedItems = null)
    {
        const string d_method_name = "ContainerUtils.RemoveRemaining";
        if (requiredAmount <= 0)
        {
            return 0;
        }

        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog($"{d_method_name} | Trying to remove {requiredAmount} {itemValue.ItemClass.GetItemName()}");
        }

        var originalAmountNeeded = requiredAmount;
        var containerStorage = GetAvailableStorages() ?? Enumerable.Empty<ITileEntityLootable>();

        foreach (var tileEntityLootable in containerStorage)
        {
            var newRequiredAmount = RemoveItems(tileEntityLootable.items, itemValue, requiredAmount, ignoreModdedItems, removedItems);
            if (requiredAmount != newRequiredAmount)
            {
                tileEntityLootable.SetModified();
            }

            requiredAmount = newRequiredAmount;
            if (requiredAmount <= 0)
            {
                break;
            }
        }

        var removedFromContainers = originalAmountNeeded - requiredAmount;
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog($"{d_method_name} | Containers | Removed {removedFromContainers} {itemValue.ItemClass.GetItemName()}");
        }

        if (requiredAmount <= 0 || !ModConfig.PullFromVehicleStorage())
        {
            return removedFromContainers;
        }

        var vehicleStorages = VehicleUtils.GetAvailableVehicleStorages() ?? Enumerable.Empty<EntityVehicle>();
        foreach (var vehicle in vehicleStorages)
        {
            var newRequiredAmount = RemoveItems(vehicle.bag.items, itemValue, requiredAmount, ignoreModdedItems, removedItems);
            if (newRequiredAmount != requiredAmount)
            {
                vehicle.SetBagModified();
            }

            requiredAmount = newRequiredAmount;
            if (requiredAmount <= 0)
            {
                break;
            }
        }

        var totalRemoved = originalAmountNeeded - requiredAmount;
        if (LogUtil.IsDebug())
        {
            LogUtil.DebugLog($"{d_method_name} | Vehicles | Removed {totalRemoved - removedFromContainers} {itemValue.ItemClass.GetItemName()}");
        }

        return totalRemoved;
    }
}