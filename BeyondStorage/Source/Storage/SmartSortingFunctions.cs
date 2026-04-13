using System;
using System.Collections.Generic;
using BeyondStorage.Source.Data;
using BeyondStorage.Source.Game.UI;
using BeyondStorage.Source.Infrastructure;
using BeyondStorage.Source.UI;

namespace BeyondStorage.Source.Storage;

public class SmartSortingFunctions
{
    public const string MSG_SMART_PULL_LOADOUT_RESULT = "msgBeyondSmartPullLoadout_Result";
    public const string MSG_SMART_PUSH_RESULT = "msgBeyondSmartPush_Result";

    private static readonly object s_smartPullLock = new();
    private static readonly object s_smartPushLock = new();

    private static IReadOnlyList<StorageTargetAdapter> GetSmartPushTargets(StorageContext context)
        => context.GetClosestTargetContainers(ItemScope.AllItems);

    // Add alongside GetSmartPushTargets
    private static IReadOnlyList<StorageTargetAdapter> GetSmartLoadoutPullSources(StorageContext context)
        => context.GetClosestTargetContainers(ItemScope.PushableItems);

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
        var targets = GetSmartPushTargets(context);

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
        var targets = GetSmartPushTargets(context);

        PerformSmartPush(context, source, targets);
    }

    private static void SmartPushFromDroneStorage(StorageContext context, EntityDrone drone)
    {
#if DEBUG
        const string d_MethodName = nameof(SmartPushFromDroneStorage);
        ModLogger.DebugLog($"{d_MethodName}: Performing smart push from drone storage");
#endif
        var source = StorageSourceAdapterFactory.CreateDroneStorageSourceAdapter(context, drone);
        var targets = GetSmartPushTargets(context);

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
        var targets = GetSmartPushTargets(context);

        PerformSmartPush(context, source, targets);
    }

    public static void SmartVehicleLoadoutPull()
    {
        const string d_MethodName = nameof(SmartVehicleLoadoutPull);
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
        var loadout = StorageSourceAdapterFactory.CreateVehicleStorageSourceAdapter(context, vehicle);
        var sources = GetSmartLoadoutPullSources(context);

        PerformSmartLoadoutPull(context, loadout, sources);
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
        var targets = GetSmartPushTargets(context);

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
        var targets = GetSmartPushTargets(context);

        PerformSmartPush(context, source, targets);
    }

    private static void PerformSmartLoadoutPull<T>(StorageContext context, StorageSourceAdapter<T> loadout, IReadOnlyList<StorageTargetAdapter> sources) where T : class
    {
        const string d_MethodName = nameof(PerformSmartLoadoutPull);

        lock (s_smartPullLock)
        {
            if (loadout == null)
            {
                ModLogger.DebugLog($"{d_MethodName}: Loadout is null, returning");
                return;
            }

            if (sources == null || sources.Count == 0)
            {
                ModLogger.DebugLog($"{d_MethodName}: No source storages found, returning");
                return;
            }

            var state = new StorageOperationState(loadout.GetName());

            // Fill up any existing partial locked slots
            PullSourceItemsToLoadout(state, sources, loadout);

#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: {state}");
#endif

            if (state.StackCount > 0)
            {
                context.ShowLocalPlayerNotification(MSG_SMART_PULL_LOADOUT_RESULT, state.StackCount, state.MasterStorageName);
                context.InvalidateCache();
            }

            UIRefreshHelper.ValidateAndRefreshUI(context, d_MethodName);
        }
    }

    private static void PerformSmartPush<S>(StorageContext context, StorageSourceAdapter<S> source, IReadOnlyList<StorageTargetAdapter> targets) where S : class
    {
        const string d_MethodName = nameof(PerformSmartPush);

        lock (s_smartPushLock)
        {
            if (source == null)
            {
                ModLogger.DebugLog($"{d_MethodName}: Source is null, returning");
                return;
            }

            if (targets == null || targets.Count == 0)
            {
                ModLogger.DebugLog($"{d_MethodName}: No target storages found, returning");
                return;
            }

            var state = new StorageOperationState(source.GetName());

            // First fill up existing partial slots as at the start of the operation
            PushSourceItemsToTarget(state, source, targets, allowPushToEmpty: false);

            // Then fill up any empty slots, and any new partial slots that are created when partially filling those empty slots
            PushSourceItemsToTarget(state, source, targets, allowPushToEmpty: true);

#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: {state}");
#endif

            if (state.StackCount > 0)
            {
                context.ShowLocalPlayerNotification(MSG_SMART_PUSH_RESULT, state.StackCount, state.MasterStorageName, state.StorageCount);
                context.InvalidateCache();
            }

            UIRefreshHelper.ValidateAndRefreshUI(context, d_MethodName);
        }
    }

    private static void PullSourceItemsToLoadout<T>(StorageOperationState state, IReadOnlyList<StorageTargetAdapter> sources, StorageSourceAdapter<T> loadout) where T : class
    {
        const string d_MethodName = nameof(PullSourceItemsToLoadout);

        var loadoutSlots = loadout.GetLoadoutItemStacks();

        for (int i = 0; i < loadoutSlots.Length; i++)
        {
            var loadoutSlot = loadoutSlots[i];
            if (ItemX.IsEmpty(loadoutSlot))
            {
                ModLogger.DebugLog($"{d_MethodName}: Loadout slot {i} is empty, skipping");
                continue;
            }

            int maxStackSize = ItemX.MaxStackSizeOf(loadoutSlot);
            if (maxStackSize <= 0)
            {
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: Loadout slot {i} in {state.MasterStorageName} has invalid max stack size {maxStackSize}, skipping");
#endif
                continue;
            }

            int loadoutSlotRequiredAmount = maxStackSize - ItemX.CurrentStackSizeOf(loadoutSlot);

            for (int k = 0; k < sources.Count; k++)
            {
                if (loadoutSlotRequiredAmount <= 0)
                {
                    // This loadout slot is already full, move on to the next one
                    break;
                }

                var source = sources[k];
                if (source.HasSameSource(loadout))
                {
                    // Don't transfer from the loadout back to itself
                    continue;
                }

                PullToLoadoutSlots(d_MethodName, state, loadout, loadoutSlot, source, maxStackSize, ref loadoutSlotRequiredAmount);
            }
        }
    }

    private static void PushSourceItemsToTarget<S>(StorageOperationState state, StorageSourceAdapter<S> source, IReadOnlyList<StorageTargetAdapter> targets, bool allowPushToEmpty) where S : class
    {
        const string d_MethodName = nameof(PushSourceItemsToTarget);

        var sourceSlots = source.GetPushableItemStacks();

        for (int i = 0; i < sourceSlots.Length; i++)
        {
            var sourceSlot = sourceSlots[i];
            if (ItemX.IsEmpty(sourceSlot))
            {
                continue;
            }

            int maxStackSize = ItemX.MaxStackSizeOf(sourceSlot);
            if (maxStackSize <= 0)
            {
#if DEBUG
                ModLogger.DebugLog($"{d_MethodName}: Source slot {i} in {state.MasterStorageName} has invalid max stack size {maxStackSize}, skipping");
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
                    // Don't transfer back to the same source storage
                    continue;
                }
#if DEBUG
                string targetStorageName = target.GetName();
#endif

                var partialSlots = target.GetPartialSlotsFor(sourceSlot);
                var emptySlots = allowPushToEmpty ? target.GetEmptySlotsFor(sourceSlot) : (IList<ItemStack>)[];

                while ((sourceSlotRemaining > 0) && (partialSlots.Count > 0 || emptySlots.Count > 0))
                {
                    PushToTargetSlots(d_MethodName, state, source, sourceSlot, target, partialSlots, maxStackSize, ref sourceSlotRemaining);
#if DEBUG
                    ModLogger.DebugLog($"{d_MethodName}: {sourceSlotRemaining} items remaining after transfer to partial slot {targetStorageName}");
#endif
                    if (sourceSlotRemaining > 0 && allowPushToEmpty)
                    {
                        PushToTargetSlots(d_MethodName, state, source, sourceSlot, target, emptySlots, maxStackSize, ref sourceSlotRemaining);
#if DEBUG
                        ModLogger.DebugLog($"{d_MethodName}: {sourceSlotRemaining} items remaining after transfer to empty slot {targetStorageName}");
#endif
                    }
                }
            }
        }
    }

    private static void PullToLoadoutSlots<T>(string methodName, StorageOperationState state, StorageSourceAdapter<T> loadout, ItemStack loadoutSlot, StorageTargetAdapter source, int maxStackSize, ref int loadoutSlotRequiredAmount) where T : class
    {
        const int FIRST_SLOT = 0;

        int transferredToLoadout = 0;

        // Keep fetching fresh source slots until loadout slot is full or no more items available
        while (loadoutSlotRequiredAmount > 0)
        {
            // Re-fetch the populated slots on each iteration since reclassification changes internal state
            var sourceSlots = source?.GetPopulatedSlotsFor(loadoutSlot) ?? [];

            if (sourceSlots.Count <= FIRST_SLOT)
            {
                // No more source slots available for this loadout slot
                break;
            }

            var sourceSlot = sourceSlots[FIRST_SLOT];

            if (ItemX.IsEmpty(sourceSlot))
            {
#if DEBUG
                ModLogger.DebugLog($"{methodName}: Skipping empty source slot");
#endif
                // Reclassify empty slot so it doesn't appear in next iteration
                source.ReclassifySlot(sourceSlot);
                continue;
            }

            var transferAmount = TransferLoadoutSlotItems(methodName, state, loadoutSlot, sourceSlot, maxStackSize, ref loadoutSlotRequiredAmount);

            transferredToLoadout += transferAmount;

            // Reclassify source slot after transfer (might be partial now, or empty)
            if (transferAmount > 0)
            {
                source.ReclassifySlot(sourceSlot);
            }
        }

        // Mark both storage containers as modified if any transfer occurred
        if (transferredToLoadout > 0)
        {
            source.MarkModified();
            loadout.MarkModified();

            state.RecordTransfer(source, loadoutSlot, transferredToLoadout);
        }
    }

    private static void PushToTargetSlots<S>(string methodName, StorageOperationState state, StorageSourceAdapter<S> source, ItemStack sourceSlot, StorageTargetAdapter target, IList<ItemStack> targetSlots, int maxStackSize, ref int sourceSlotRemaining) where S : class
    {
        const int FIRST_SLOT = 0;

        int transferredToTarget = 0;

        while (targetSlots.Count > FIRST_SLOT && sourceSlotRemaining > 0)
        {
            var targetSlot = targetSlots[FIRST_SLOT];

            var transferAmount = TransferTargetSlotItems(methodName, state, sourceSlot, targetSlot, maxStackSize, ref sourceSlotRemaining);

            transferredToTarget += transferAmount;

            if (transferAmount > 0)
            {
                target.ReclassifySlot(targetSlot);
            }
        }

        if (transferredToTarget > 0)
        {
            source.MarkModified();
            target.MarkModified();

            state.RecordTransfer(target, sourceSlot, transferredToTarget);
        }
    }

    private static int TransferLoadoutSlotItems(string methodName, StorageOperationState state, ItemStack loadoutSlot, ItemStack sourceSlot, int maxStackSize, ref int loadoutSlotRequiredAmount)
    {
        if (loadoutSlot == null || sourceSlot == null)
        {
            return 0;
        }

        // Create a local tracking variable for the source slot's remaining amount
        int sourceSlotRemaining = ItemX.CurrentStackSizeOf(sourceSlot);

        // Limit transfer to the loadout slot's required amount
        int intendedTransfer = Math.Min(sourceSlotRemaining, loadoutSlotRequiredAmount);
        if (intendedTransfer <= 0)
        {
            return 0;
        }

        // Use a temporary variable that TransferTargetSlotItems can modify
        int tempSourceRemaining = intendedTransfer;

        // Reuse the existing transfer logic by treating loadout slot as target slot
        int actualTransferAmount = TransferTargetSlotItems(methodName, state, sourceSlot, loadoutSlot, maxStackSize, ref tempSourceRemaining);

        // Update the loadout slot required amount with actual transfer
        loadoutSlotRequiredAmount -= actualTransferAmount;

        return actualTransferAmount;
    }

    private static int TransferTargetSlotItems(string methodName, StorageOperationState state, ItemStack sourceSlot, ItemStack targetSlot, int maxStackSize, ref int sourceSlotRemaining)
    {
        if (targetSlot == null || sourceSlot == null)
        {
            return 0;
        }

        // Calculate available space in target slot
        int targetSlotSpace = maxStackSize - ItemX.CurrentStackSizeOf(targetSlot);

        int transferAmount = Math.Min(sourceSlotRemaining, targetSlotSpace);
        if (transferAmount <= 0)
        {
            return 0;
        }

#if DEBUG
        ModLogger.DebugLog($"{methodName}: Transferring {transferAmount} of {ItemX.NameOf(sourceSlot)} (storage: {state.MasterStorageName})");
#endif

        // Track target count BEFORE transfer
        int targetCountBefore = targetSlot.count;

        // Prepare empty target slot if needed (only check once)
        if (ItemX.ItemTypeOf(targetSlot) == UniqueItemTypes.EMPTY || targetSlot.count <= 0)
        {
            targetSlot.itemValue = sourceSlot.itemValue.Clone();
            targetSlot.count = 0;
            targetCountBefore = 0;  // Reset since we just set count to 0
        }

        // Apply transfer to ItemStacks
        targetSlot.count += transferAmount;

        // Calculate ACTUAL amount transferred by checking what changed
        int actualTransferAmount = targetSlot.count - targetCountBefore;

        // Update tracking variables with ACTUAL amount
        sourceSlotRemaining -= actualTransferAmount;
        sourceSlot.count = sourceSlotRemaining;

        return actualTransferAmount;
    }
}