using System;
using System.Collections.Generic;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.TileEntities;

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
                ModLogger.DebugLog($"  Slot {i+1}/{maxItems}: {itemStack.count}x {ItemX.NameOf(itemStack)}");
            }
            else
            {
                ModLogger.DebugLog($"  Slot {i+1}/{maxItems}: Empty");
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

            ModLogger.DebugLog($"  Container {i+1}/{maxContainers}: {containerName} ({maxItems} slots) Distance: {targets[i].Distance:0.###}");

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
        LogTargetItems(d_MethodName, targets);
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

        PushToExistingPartialStacks(source, targets);

        context.InvalidateCache();
    }

    private static void PushToExistingPartialStacks<T, S>(StorageSourceAdapter<T> source, IReadOnlyList<StorageTargetAdapter<S>> targets) where T : class where S : class
    {
#if DEBUG
        const string d_MethodName = nameof(PushToExistingPartialStacks);
#endif

        var sourceItems = source.GetPullableItemStacks();
#if DEBUG
        LogSourceItems(d_MethodName, sourceItems);
#endif

        for (int i = 0; i< sourceItems.Length; i++)
        {
            var sourceStack = sourceItems[i];
            if (ItemX.IsEmpty(sourceStack))
                continue;

            foreach (var target in targets)
            {
                var partialSlots = target.GetPartialSlotsFor(sourceStack);
                if (partialSlots.Count == 0)
                    continue;

                if (sourceStack.count <= 0)
                    break;
            }
        }
    }
}
