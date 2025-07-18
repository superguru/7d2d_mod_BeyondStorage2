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

    public static IEnumerable<ItemStack> GetItemStacks()
    {
        var containerStorage = GetAvailableContainerStorages();
        var results = containerStorage?.SelectMany(lootable => lootable?.items) ?? [];

        if (ModConfig.PullFromVehicleStorage())
        {
            var vehicleStorage = VehicleUtils.GetAvailableVehicleStorages() ?? [];
            var vehicleResults = vehicleStorage?.SelectMany(vehicle => vehicle?.bag?.GetSlots() ?? Enumerable.Empty<ItemStack>()) ?? [];

            results.Concat(vehicleResults);
        }

        if (ModConfig.PullFromWorkstationOutputs())
        {
            var workstationOutputs = WorkstationUtils.GetAvailableWorkstationOutputs() ?? [];
            var workstationResults = workstationOutputs?.SelectMany(workstation => workstation?.Output ?? Enumerable.Empty<ItemStack>()) ?? [];

            results = results.Concat(workstationResults);
        }

        return results;
    }

    public static bool HasItem(ItemValue itemValue)
    {
        if (itemValue == null || itemValue.IsEmpty())
        {
            LogUtil.DebugLog($"HasItem Item is null or Empty");
            return false;
        }

        var containerStorage = GetAvailableContainerStorages();
        bool containerHas = containerStorage != null &&
            containerStorage.SelectMany(lootable => lootable?.items)
                            .Any(stack => stack?.itemValue?.type == itemValue.type);

        if (containerHas)
        {
            return true;
        }

        if (ModConfig.PullFromVehicleStorage())
        {

            var vehicleStorage = VehicleUtils.GetAvailableVehicleStorages();
            bool vehicleHas = vehicleStorage != null &&
                vehicleStorage.SelectMany(vehicle => vehicle?.bag?.items ?? Enumerable.Empty<ItemStack>())
                              .Any(stack => stack?.itemValue?.type == itemValue.type);

            if (vehicleHas)
            {
                return true;
            }
        }

        if (ModConfig.PullFromWorkstationOutputs())
        {
            var workstationOutputs = WorkstationUtils.GetAvailableWorkstationOutputs();
            bool workstationHas = workstationOutputs != null &&
                workstationOutputs.SelectMany(workstation => workstation?.Output ?? Enumerable.Empty<ItemStack>())
                                  .Any(stack => stack?.itemValue?.type == itemValue.type);

            if (workstationHas)
            {
                return true;
            }
        }

        return false;
    }

    public static int GetItemCount(ItemValue itemValue)
    {
        if (itemValue == null || itemValue.IsEmpty())
        {
            LogUtil.DebugLog($"GetItemCount Item is null or Empty");
            return 0;
        }

        static int CountItems(IEnumerable<ItemStack> stacks, int type)
            => stacks?.Where(stack => stack?.itemValue?.type == type).Sum(stack => stack?.count) ?? 0;

        var containerStorage = GetAvailableContainerStorages();
        int containerCount = CountItems(containerStorage?.SelectMany(lootable => lootable?.items), itemValue.type);
        LogUtil.DebugLog($"Container Storage count is {containerCount}");
        int totalCount = containerCount;

        if (ModConfig.PullFromVehicleStorage())
        {
            LogUtil.DebugLog("Will try to pull from Vehicle Storage");
            var vehicleStorage = VehicleUtils.GetAvailableVehicleStorages();
            int vehicleCount = CountItems(vehicleStorage?.SelectMany(vehicle => vehicle?.bag?.items ?? Enumerable.Empty<ItemStack>()), itemValue.type);
            LogUtil.DebugLog($"Vehicle Storage count is {vehicleCount}");

            totalCount += vehicleCount;
        }

        if (ModConfig.PullFromWorkstationOutputs())
        {
            LogUtil.DebugLog("Will try to pull from Workstation Outputs");
            var workstationOutputs = WorkstationUtils.GetAvailableWorkstationOutputs();
            int workstationOutputsCount = CountItems(workstationOutputs?.SelectMany(workstation => workstation?.Output ?? Enumerable.Empty<ItemStack>()), itemValue.type);
            LogUtil.DebugLog($"Workstation Output count is {workstationOutputsCount}");

            totalCount += workstationOutputsCount;
        }

        return totalCount;
    }

    private static IEnumerable<ITileEntityLootable> GetAvailableContainerStorages()
    {
        var player = GameManager.Instance.World.GetPrimaryPlayer();
        var playerPos = player.position;
        var configRange = ModConfig.Range();
        var configOnlyCrates = ModConfig.OnlyStorageCrates();
        var internalLocalUserIdentifier = PlatformManager.InternalLocalUserIdentifier;

        var chunkCacheCopy = GameManager.Instance.World.ChunkCache.GetChunkArrayCopySync();

        foreach (var tileEntity in chunkCacheCopy.Where(chunk => chunk != null).SelectMany(chunk => chunk.GetTileEntities().list))
        {
            bool isInRange = (configRange <= 0 || Vector3.Distance(playerPos, tileEntity.ToWorldPos()) < configRange);
            if (!isInRange)
            {
                continue;
            }

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
            // TODO: You might want to comment the following line out while debugging new features
            // LogUtil.DebugLog($"TEL: {tileEntityLootable}; Locked Count: {LockedTileEntities.Count}; {tileEntity.IsUserAccessing()}");
#endif
            if (tileEntity.TryGetSelfOrFeature(out ILockable tileLockable))
            {
                if (tileLockable.IsLocked() && !tileLockable.IsUserAllowed(internalLocalUserIdentifier))
                {
                    continue;
                }
            }

            if (LockedTileEntities.Count > 0)
            {
                var pos = tileEntityLootable.ToWorldPos();
                if (LockedTileEntities.TryGetValue(pos, out int entityId) && entityId != player.entityId)
                {
                    continue;
                }
            }

            yield return tileEntityLootable;
        }
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
                var countToRemove = Mathf.Min(stack.count, stillNeeded);
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
        const string d_method_name = "RemoveRemaining";

        if (itemValue == null || itemValue.IsEmpty() || stillNeeded <= 0)
        {
#if DEBUG
            LogUtil.DebugLog($"{d_method_name} | Null or empty item; stillNeeded {stillNeeded}; null is {itemValue == null}");
#endif
            return 0;
        }

        var itemName = itemValue.ItemClass.GetItemName();

        LogUtil.DebugLog($"{d_method_name} | Trying to remove {stillNeeded} {itemName}");

        int originalAmountNeeded = stillNeeded;
        int totalRemoved = 0;

        var containerStorage = GetAvailableContainerStorages() ?? [];

        foreach (var tileEntityLootable in containerStorage)
        {
            var newRequiredAmount = RemoveItems(tileEntityLootable.items, itemValue, stillNeeded, ignoreModdedItems, removedItems);
            if (stillNeeded != newRequiredAmount)
            {
                tileEntityLootable.SetModified();
            }

            stillNeeded = newRequiredAmount;
            if (stillNeeded == 0)
            {
                break;
            }
        }
#if DEBUG
        if (stillNeeded < 0)
        {
            LogUtil.DebugLog($"{d_method_name} | stillNeeded after Containers should not be negative, but is {stillNeeded}");
            return 0;  // Not sure what to do here, but returning 0 to avoid negative stillNeeded
        }
#endif
        totalRemoved = originalAmountNeeded - stillNeeded;
        LogUtil.DebugLog($"{d_method_name} | Containers | Removed {totalRemoved} {itemName}, stillNeeded {stillNeeded}");

        if (stillNeeded == 0)
        {
            return totalRemoved;
        }

        if (ModConfig.PullFromVehicleStorage())
        {
            int neededBeforeVehicles = stillNeeded;
            var vehicleStorages = VehicleUtils.GetAvailableVehicleStorages() ?? [];
            foreach (var vehicle in vehicleStorages)
            {
                var newRequiredAmount = RemoveItems(vehicle.bag.items, itemValue, stillNeeded, ignoreModdedItems, removedItems);
                if (stillNeeded != newRequiredAmount)
                {
                    vehicle.SetBagModified();
                }

                stillNeeded = newRequiredAmount;
                if (stillNeeded == 0)
                {
                    break;
                }
            }
#if DEBUG
            if (stillNeeded < 0)
            {
                LogUtil.DebugLog($"{d_method_name} | stillNeeded after Vehicles should not be negative, but is {stillNeeded}");
                return 0;  // Not sure what to do here, but returning 0 to avoid negative stillNeeded
            }
#endif
            int vehiclesRemoved = neededBeforeVehicles - stillNeeded;
            totalRemoved = originalAmountNeeded - stillNeeded;
            LogUtil.DebugLog($"{d_method_name} | Vehicles | Removed {vehiclesRemoved} {itemName}, stillNeeded {stillNeeded}");
        }

        if (ModConfig.PullFromWorkstationOutputs())
        {
            int beforeWorkstationOutputs = stillNeeded;
            var workstationOutputs = WorkstationUtils.GetAvailableWorkstationOutputs() ?? [];
            foreach (var workstation in workstationOutputs)
            {
                var newRequiredAmount = RemoveItems(workstation.Output, itemValue, stillNeeded, ignoreModdedItems, removedItems);

                LogUtil.DebugLog($"{d_method_name} | Workstation stillNeeded {stillNeeded} newRequiredAmount {newRequiredAmount}");

                if (stillNeeded != newRequiredAmount)
                {
                    LogUtil.DebugLog($"{d_method_name} | Marking Workstation {workstation.GetType().Name} as modified");
                    MarkWorkstationModified(workstation);
                }

                stillNeeded = newRequiredAmount;
                if (stillNeeded == 0)
                {
                    break;
                }
            }
#if DEBUG
            if (stillNeeded < 0)
            {
                LogUtil.DebugLog($"{d_method_name} | stillNeeded after WorkstationOutputs should not be negative, but is {stillNeeded}");
                return 0;  // Not sure what to do here, but returning 0 to avoid negative stillNeeded
            }
#endif
            int workstationOutputsRemoved = beforeWorkstationOutputs - stillNeeded;
            totalRemoved = originalAmountNeeded - stillNeeded;
            LogUtil.DebugLog($"{d_method_name} | WorkstationOutputs | Removed {workstationOutputsRemoved} {itemName}, stillNeeded {stillNeeded}");
        }

        return totalRemoved;
    }

    private static void MarkWorkstationModified(TileEntityWorkstation workstation)
    {
        // This method of is used when items are removed from a workstation's output, such as when pulling items from the workstation
#if DEBUG
        if (workstation == null)
        {
            LogUtil.Error("MarkWorkstationModified: workstation is null");
            return;
        }
#endif
        workstation.SetChunkModified();
        workstation.SetModified();

        string blockName = GameManager.Instance.World.GetBlock(workstation.ToWorldPos()).Block.GetBlockName();
        WorkstationData workstationData = CraftingManager.GetWorkstationData(blockName);
        if (workstationData != null)
        {
            string text = ((workstationData.WorkstationWindow != "") ? workstationData.WorkstationWindow : $"workstation_{blockName}");
#if DEBUG
            LogUtil.DebugLog($"MarkWorkstationModified: blockName {blockName}, text {text}");
#endif
            var player = GameManager.Instance.World.GetPrimaryPlayer();

            if (player.windowManager.HasWindow(text))
            {
#if DEBUG
                LogUtil.DebugLog($"MarkWorkstationModified: Found window for {text}");
#endif
                var workstation_windowgroup = ((XUiC_WorkstationWindowGroup)((XUiWindowGroup)player.windowManager.GetWindow(text)).Controller);
                if (workstation_windowgroup == null)
                {
                    LogUtil.Error($"MarkWorkstationModified: workstation_windowgroup is null for {text}");
                    return;
                }

                if (workstation_windowgroup.WorkstationData == null)
                {
                    LogUtil.Error($"MarkWorkstationModified: workstation_windowgroup.WorkstationData is null for {text}");
                    return;
                }

                var w = player.windowManager.GetWindow(text);
                if (w == null)
                {
                    LogUtil.Error($"MarkWorkstationModified: Window {text} is null");
                    return;
                }

                if (!w.isShowing)
                {
                    //LogUtil.Error($"MarkWorkstationModified: Window {text} is not showing");
                    return;
                }

                workstation_windowgroup.syncUIfromTE();
#if DEBUG
                LogUtil.DebugLog($"MarkWorkstationModified: Synced UI from TE for {text}");
#endif
            }
            else
            {
                LogUtil.Error($"MarkWorkstationModified: No WorkstationData found for block '{blockName}'");
            }
        }
    }
}