using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Source.Game.UI;

namespace BeyondStorage.Scripts.Storage;

public class SmartSortingFunctions
{
#if DEBUG
    private static void LogSourceItems(string d_MethodName, ItemStack[] sourceItems)
    {
        if (sourceItems == null || sourceItems.Length == 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: No source items found.");
            return;
        }

        ModLogger.DebugLog($"{d_MethodName}: Source items:");
        int maxItems = sourceItems.Length;
        for (int i = 0; i < maxItems; i++)
        {
            var itemStack = sourceItems[i];
            if (itemStack != null)
            {
                ModLogger.DebugLog($"  Slot {i + 1}/{maxItems}: {itemStack.count}x {ItemX.NameOf(itemStack)}");
            }
            else
            {
                ModLogger.DebugLog($"  Slot {i + 1}/{maxItems}: Empty");
            }
        }
    }
#endif

#if DEBUG
    private static void LogTargetItems(string d_MethodName, IReadOnlyList<StorageTargetAdapter<ITileEntityLootable>> targets)
    {
        if (targets == null || targets.Count == 0)
        {
            ModLogger.DebugLog($"{d_MethodName}: No target containers found.");
            return;
        }

        ModLogger.DebugLog($"{d_MethodName}: Target containers:");
        int maxContainers = targets.Count;
        for (int i = 0; i < maxContainers; i++)
        {
            var target = targets[i];

            string containerName = target.GetTargetName();

            var items = target.GetAllSlotItemStacks();
            int maxItems = items.Length;

            ModLogger.DebugLog($"  Container {i + 1}/{maxContainers}: {containerName} ({maxItems} slots) Distance: {targets[i].Distance:0.###}");

            for (int j = 0; j < maxItems; j++)
            {
                var itemStack = items[j];
                if (itemStack == null || itemStack.count <= 0)
                {
                    ModLogger.DebugLog($"    Slot {j + 1}/{maxItems}: Empty");
                }
                else
                {
                    ModLogger.DebugLog($"    Slot {j + 1}/{maxItems}: {itemStack.count}x {ItemX.NameOf(itemStack)}");
                }
            }
        }
    }
#endif

    public static void SmartCollectorPush()
    {
#if DEBUG
        const string d_MethodName = nameof(SmartCollectorPush);
#endif

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

    public static void SmartPlayerInventoryPush()
    {
        const string d_MethodName = nameof(SmartPlayerInventoryPush);

#if DEBUG
        ModLogger.DebugLog($"{d_MethodName}: Performing smart player inventory push");
#endif

        if (!ValidationHelper.ValidateStorageContext(d_MethodName, out StorageContext context))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning");
            return;
        }

        var source = StorageSourceAdapterFactory.CreatePlayerLootableSourceAdapter(context, context.Player);

        var targets = context.GetClosestTargetContainers();
#if DEBUG
        //LogTargetItems(d_MethodName, targets);
#endif

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

        PushToExistingPartialSlots(source, targets);
        PushToOverflowSlots(source, targets);

        context.InvalidateCache();
    }

    private static void PushToExistingPartialSlots<T, S>(StorageSourceAdapter<T> source, IReadOnlyList<StorageTargetAdapter<S>> targets) where T : class where S : class
    {
#if DEBUG
        const string d_MethodName = nameof(PushToExistingPartialSlots);
#endif

        var sourceSlots = source.GetPullableItemStacks();
#if DEBUG
        LogSourceItems(d_MethodName, sourceSlots);
#endif

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
                var target = targets[k];

                var partialSlots = target.GetPartialSlotsFor(sourceSlot);
                TransferToTargetSlots(d_MethodName, source, sourceSlot, target, partialSlots, ref sourceSlotRemaining);
            }
        }
    }

    private static void PushToOverflowSlots<T, S>(StorageSourceAdapter<T> source, IReadOnlyList<StorageTargetAdapter<S>> targets) where T : class where S : class
    {
#if DEBUG
        const string d_MethodName = nameof(PushToOverflowSlots);
#endif
        var sourceSlots = source.GetPullableItemStacks();
#if DEBUG
        LogSourceItems(d_MethodName, sourceSlots);
#endif

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

                var emptySlots = target.GetEmptySlotsFor(sourceSlot);
                TransferToTargetSlots(d_MethodName, source, sourceSlot, target, emptySlots, ref sourceSlotRemaining);
            }
        }
    }

    private static void TransferToTargetSlots<T, S>(string methodName, StorageSourceAdapter<T> source, ItemStack sourceSlot, StorageTargetAdapter<S> target, IList<ItemStack> targetSlots, ref int sourceSlotRemaining) where T : class where S : class
    {
        int maxStackSize = ItemX.MaxStackSizeOf(sourceSlot);
        int transferredToTarget = 0;

        for (int j = 0; j < targetSlots.Count; j++)
        {
            if (sourceSlotRemaining <= 0)
            {
                break;
            }

            var targetSlot = targetSlots[j];

            int targetSlotSpace = maxStackSize - ItemX.CurrentStackSizeOf(targetSlot);
            if (targetSlotSpace <= 0)
            {
                continue;
            }

            TransferSlotItems(methodName, sourceSlot, targetSlot, j, target.GetTargetName(), targetSlotSpace, ref sourceSlotRemaining, ref transferredToTarget);
        }

        if (transferredToTarget > 0)
        {
            source.MarkModified();
            target.MarkModified();
        }
    }

    private static void TransferSlotItems(string methodName, ItemStack sourceSlot, ItemStack targetSlot, int slotIndex, string containerName, int targetSlotSpace, ref int sourceSlotRemaining, ref int transferredToTarget)
    {
        int transferAmount = Math.Min(sourceSlotRemaining, targetSlotSpace);
#if DEBUG
        ModLogger.DebugLog($"{methodName}: Transferring {transferAmount} of {ItemX.NameOf(sourceSlot)} to target slot {slotIndex} in container {containerName}");
#endif
        if (ItemX.ItemTypeOf(targetSlot) == UniqueItemTypes.EMPTY || targetSlot?.count <= 0)
        {
#if DEBUG
            ModLogger.DebugLog($"{methodName}: Target slot {slotIndex} in container {containerName} is empty, cloning source slot item type");
#endif
            targetSlot.itemValue = sourceSlot.itemValue.Clone();
            targetSlot.count = 0;
        }

        targetSlot.count += transferAmount;
        sourceSlotRemaining -= transferAmount;
        sourceSlot.count = sourceSlotRemaining;

        transferredToTarget += transferAmount;
    }
}
