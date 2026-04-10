using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.UI;
using BeyondStorage.Source.Game.UI;

namespace BeyondStorage.Scripts.Storage;

public class SmartSortingFunctions
{
    public static void SmartCollectorPush()
    {
        const string d_MethodName = nameof(SmartCollectorPush);

        if (!ValidationHelper.ValidateStorageContext(d_MethodName, out StorageContext context))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning");
            return;
        }

        var collector = WindowStateManager.GetOpenCollectorTileEntity();
        if (collector == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: No open collector found, returning");
            return;
        }

        var source = StorageSourceAdapterFactory.CreateCollectorStorageSourceAdapter(context, collector);
        var targets = context.GetClosestTargetContainers();

        PerformSmartPush(context, source, targets);
    }

    public static void SmartLootWindowPush()
    {
        const string d_MethodName = nameof(SmartLootWindowPush);

        if (!ValidationHelper.ValidateStorageContext(d_MethodName, out StorageContext context))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning");
            return;
        }

        var lootable = WindowStateManager.GetOpenWindowLootable();
        if (lootable == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: No open lootable window found, returning");
            return;
        }

        var drone = WindowStateManager.GetDroneForOpenStorageContainer();
        if (drone == null)
        {
            SmartPushFromPlayerCreatedStorage(context, lootable);
        }
        else
        {
            SmartPushFromDroneStorage(context, drone);
        }

    }

    private static void SmartPushFromPlayerCreatedStorage(StorageContext context, ITileEntityLootable lootable)
    {
#if DEBUG
        const string d_MethodName = nameof(SmartPushFromPlayerCreatedStorage);
        ModLogger.DebugLog($"{d_MethodName}: Performing smart push from player created storage");
#endif
        var source = StorageSourceAdapterFactory.CreateLootableStorageSourceAdapter(context, lootable);
        var targets = context.GetClosestTargetContainers();

        PerformSmartPush(context, source, targets);
    }

    private static void SmartPushFromDroneStorage(StorageContext context, EntityDrone drone)
    {
#if DEBUG
        const string d_MethodName = nameof(SmartPushFromDroneStorage);
        ModLogger.DebugLog($"{d_MethodName}: Performing smart push from drone storage");
#endif
        var source = StorageSourceAdapterFactory.CreateDroneStorageSourceAdapter(context, drone);
        var targets = context.GetClosestTargetContainers();

        PerformSmartPush(context, source, targets);
    }

    public static void SmartPlayerInventoryPush()
    {
        const string d_MethodName = nameof(SmartPlayerInventoryPush);

        if (!ValidationHelper.ValidateStorageContext(d_MethodName, out StorageContext context))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning");
            return;
        }

        var source = StorageSourceAdapterFactory.CreatePlayerLootableSourceAdapter(context, context.Player);
        var targets = context.GetClosestTargetContainers();

        PerformSmartPush(context, source, targets);
    }

    public static void SmartVehiclePush()
    {
        const string d_MethodName = nameof(SmartVehiclePush);

        if (!ValidationHelper.ValidateStorageContext(d_MethodName, out StorageContext context))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning");
            return;
        }

        var vehicle = WindowStateManager.GetOpenVehicleTileEntity();
        if (vehicle == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: No open vehicle found, returning");
            return;
        }

        var source = StorageSourceAdapterFactory.CreateVehicleStorageSourceAdapter(context, vehicle);
        var targets = context.GetClosestTargetContainers();

        PerformSmartPush(context, source, targets);
    }


    public static void SmartWorkstationOutputPush()
    {
        const string d_MethodName = nameof(SmartWorkstationOutputPush);

        if (!ValidationHelper.ValidateStorageContext(d_MethodName, out StorageContext context))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning");
            return;
        }

        var workstation = WindowStateManager.GetOpenWorkstationTileEntity();
        if (workstation == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: No open workstation found, returning");
            return;
        }

        var source = StorageSourceAdapterFactory.CreateWorkstationStorageSourceAdapter(context, workstation);
        var targets = context.GetClosestTargetContainers();

        PerformSmartPush(context, source, targets);
    }

    private static void PerformSmartPush<T, S>(StorageContext context, StorageSourceAdapter<T> source, IReadOnlyList<StorageTargetAdapter<S>> targets) where T : class where S : class
    {
        const string d_MethodName = nameof(PerformSmartPush);

        if (source == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: Source is null, returning");
            return;
        }

        if (targets == null || targets.Count == 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: No target containers found, returning");
            return;
        }

        context.ShowLocalPlayerNotification("msgBeyondSmartPush_Pushing", null, source.GetName(), targets.Count);

        // First fill up existing partial slots as at the start of the operation
        PushSourceItemsToTarget(source, targets, allowPushtoEmpty: false);

        // Then fill up any empty slots, and any new partial slots that are created when partially filling those empty slots
        PushSourceItemsToTarget(source, targets, allowPushtoEmpty: true);

        UIRefreshHelper.ValidateAndRefreshUI(context, d_MethodName);
    }

    private static void PushSourceItemsToTarget<T, S>(StorageSourceAdapter<T> source, IReadOnlyList<StorageTargetAdapter<S>> targets, bool allowPushtoEmpty) where T : class where S : class
    {
        const string d_MethodName = nameof(PushSourceItemsToTarget);

        var sourceSlots = source.GetPushableItemStacks();

        for (int i = 0; i < sourceSlots.Length; i++)
        {
            var sourceSlot = sourceSlots[i];
            if (ItemX.IsEmpty(sourceSlot))
                continue;

            int maxStackSize = ItemX.MaxStackSizeOf(sourceSlot);
            if (maxStackSize <= 0)
            {
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: Source slot {i} has invalid max stack size {maxStackSize}, skipping");
#endif
                continue;
            }

            int sourceSlotRemaining = ItemX.CurrentStackSizeOf(sourceSlot);

            for (int k = 0; k < targets.Count; k++)
            {
                if (sourceSlotRemaining <= 0)
                {
                    break;
                }

                var target = targets[k];
                if (target.HasSameSource(source))
                {
                    continue;
                }

                var partialSlots = target.GetPartialSlotsFor(sourceSlot);
                var emptySlots = allowPushtoEmpty ? target.GetEmptySlotsFor(sourceSlot) : [];

                while ((sourceSlotRemaining > 0) && (partialSlots.Count > 0 || emptySlots.Count > 0))
                {
                    // Try to transfer to any new partial slots that may have opened up after previous transfers in this loop
                    TransferToTargetSlots(d_MethodName, source, sourceSlot, target, partialSlots, ref sourceSlotRemaining);
#if DEBUG
                    ModLogger.DebugLog($"{d_MethodName}: {sourceSlotRemaining} items remaining after partial slot transfer to target containerXX {target.GetTargetName()}");
#endif
                    if (sourceSlotRemaining > 0)
                    {
                        // If there are still items remaining, try to transfer to empty slots
                        TransferToTargetSlots(d_MethodName, source, sourceSlot, target, emptySlots, ref sourceSlotRemaining);
#if DEBUG
                        ModLogger.DebugLog($"{d_MethodName}: {sourceSlotRemaining} items remaining after empty slot transfer to target containerXX {target.GetTargetName()}");
#endif
                        // At this point, there might be less empty slots, and more partial slots
                    }
                }
            }
        }
    }

    private static void TransferToTargetSlots<T, S>(string methodName, StorageSourceAdapter<T> source, ItemStack sourceSlot, StorageTargetAdapter<S> target, IList<ItemStack> targetSlots, ref int sourceSlotRemaining) where T : class where S : class
    {
        const int FIRST_SLOT = 0;

        int maxStackSize = ItemX.MaxStackSizeOf(sourceSlot);
        int transferredToTarget = 0;

        while (targetSlots.Count > FIRST_SLOT && sourceSlotRemaining > 0)
        {
            var targetSlot = targetSlots[FIRST_SLOT];
            int targetSlotSpace = maxStackSize - ItemX.CurrentStackSizeOf(targetSlot);

            var transferAmount = TransferSlotItems(methodName, sourceSlot, targetSlot, target.GetTargetName(), targetSlotSpace, ref sourceSlotRemaining, ref transferredToTarget);
            if (transferAmount > 0)
            {
                target.ReclassifySlot(targetSlots, targetSlot, FIRST_SLOT);
            }
        }

        if (transferredToTarget > 0)
        {
            source.MarkModified();
            target.MarkModified();
        }
    }

    private static int TransferSlotItems(string methodName, ItemStack sourceSlot, ItemStack targetSlot, string containerName, int targetSlotSpace, ref int sourceSlotRemaining, ref int transferredToTarget)
    {
        int transferAmount = Math.Min(sourceSlotRemaining, targetSlotSpace);
#if DEBUG
        ModLogger.DebugLog($"{methodName}: Transferring {transferAmount} of {ItemX.NameOf(sourceSlot)} to target {containerName}");
#endif

        if (ItemX.ItemTypeOf(targetSlot) == UniqueItemTypes.EMPTY || targetSlot?.count <= 0)
        {
            // Target slot in container is empty, cloning source slot item type
            targetSlot.itemValue = sourceSlot.itemValue.Clone();
            targetSlot.count = 0;
        }

        targetSlot.count += transferAmount;
        sourceSlotRemaining -= transferAmount;
        sourceSlot.count = sourceSlotRemaining;

        transferredToTarget += transferAmount;

        return transferAmount;
    }
}
