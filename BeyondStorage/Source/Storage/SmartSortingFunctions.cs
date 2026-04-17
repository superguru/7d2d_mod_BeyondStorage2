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
        => context.GetClosestStorageSources(StorageSourcePolicy.SmartPushSources, ItemScope.AllItems);

    private static IReadOnlyList<StorageTargetAdapter> GetSmartLoadoutPullSources(StorageContext context)
        => context.GetClosestStorageSources(StorageSourcePolicy.SmartLoadoutPullSources, ItemScope.PushableItems);

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

        PerformSmartPush(d_MethodName, context, source, targets);
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
        if (drone != null)
        {
            SmartPushFromDroneStorage(context, drone);
        }
        else
        {
            SmartPushFromPlayerCreatedStorage(context, lootable);
        }
    }

    private static void SmartPushFromPlayerCreatedStorage(StorageContext context, ITileEntityLootable lootable)
    {
        const string d_MethodName = nameof(SmartPushFromPlayerCreatedStorage);

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Performing smart push from player created storage");
#endif
        var source = StorageSourceAdapterFactory.CreateLootableStorageSourceAdapter(context, lootable);
        var targets = GetSmartPushTargets(context);

        PerformSmartPush(d_MethodName, context, source, targets);
    }

    public static void SmartDroneInventoryLoadoutPull()
    {
        const string d_MethodName = nameof(SmartDroneInventoryLoadoutPull);

        if (!ValidationHelper.ValidateStorageContext(d_MethodName, out StorageContext context))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning");
            return;
        }
        var drone = WindowStateManager.GetDroneForOpenStorageContainer();
        if (drone == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: No open drone storage found, returning");
            return;
        }

        var loadout = StorageSourceAdapterFactory.CreateDroneStorageSourceAdapter(context, drone);
        var sources = GetSmartLoadoutPullSources(context);

        PerformSmartLoadoutPull(d_MethodName, context, loadout, sources);
    }

    private static void SmartPushFromDroneStorage(StorageContext context, EntityDrone drone)
    {
        const string d_MethodName = nameof(SmartPushFromDroneStorage);

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: Performing smart push from drone storage");
#endif
        var source = StorageSourceAdapterFactory.CreateDroneStorageSourceAdapter(context, drone);
        var targets = GetSmartPushTargets(context);

        PerformSmartPush(d_MethodName, context, source, targets);
    }

    public static void SmartPlayerInventoryLoadoutPull()
    {
        const string d_MethodName = nameof(SmartPlayerInventoryLoadoutPull);

        if (!ValidationHelper.ValidateStorageContext(d_MethodName, out StorageContext context))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning");
            return;
        }

        var loadout = StorageSourceAdapterFactory.CreatePlayerLootableSourceAdapter(context, context.Player);
        var sources = GetSmartLoadoutPullSources(context);

        PerformSmartLoadoutPull(d_MethodName, context, loadout, sources);
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

        PerformSmartPush(d_MethodName, context, source, targets);
    }

    public static void SmartVehicleLoadoutPull()
    {
        const string d_MethodName = nameof(SmartVehicleLoadoutPull);

        ModLogger.DebugLog($"{d_MethodName}: Starting smart loadout pull for vehicle2");

        if (!ValidationHelper.ValidateStorageContext(d_MethodName, out StorageContext context))
        {
            ModLogger.DebugLog($"{d_MethodName}: Validation failed, returning");
            return;
        }

        var vehicle = WindowStateManager.GetOpenVehicleTileEntity();
        if (vehicle == null)
        {
            ModLogger.DebugLog($"{d_MethodName}: No open vehicle2 found, returning");
            return;
        }

        var loadout = StorageSourceAdapterFactory.CreateVehicleStorageSourceAdapter(context, vehicle);
        var sources = GetSmartLoadoutPullSources(context);

        PerformSmartLoadoutPull(d_MethodName, context, loadout, sources);
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
            ModLogger.DebugLog($"{d_MethodName}: No open vehicle2 found, returning");
            return;
        }

        var source = StorageSourceAdapterFactory.CreateVehicleStorageSourceAdapter(context, vehicle);
        var targets = GetSmartPushTargets(context);

        PerformSmartPush(d_MethodName, context, source, targets);
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

        PerformSmartPush(d_MethodName, context, source, targets);
    }

    private static void PerformSmartLoadoutPull<T>(string methodName, StorageContext context, StorageSourceAdapter<T> loadout, IReadOnlyList<StorageTargetAdapter> sources) where T : class
    {
        lock (s_smartPullLock)
        {
            if (loadout == null)
            {
                ModLogger.DebugLog($"{methodName}: Loadout is null, returning");
                return;
            }

            if (sources == null || sources.Count == 0)
            {
                ModLogger.DebugLog($"{methodName}: No source storages found, returning");
                return;
            }

            var state = new StorageOperationState(loadout.GetName(), SmartTransferOperation.TopUp);

            // Fill up any existing partial locked slots
            PullSourceItemsToLoadout(methodName, state, sources, loadout);

            ModLogger.DebugLog($"{methodName}: {state}");

            if (state.StackCount > 0)
            {
                context.ShowLocalPlayerNotification(MSG_SMART_PULL_LOADOUT_RESULT, state.StackCount, state.MasterStorageName);
                context.InvalidateCache();
            }

            UIRefreshHelper.ValidateAndRefreshUI(context, methodName);
        }
    }

    private static void PerformSmartPush<S>(string methodName, StorageContext context, StorageSourceAdapter<S> source, IReadOnlyList<StorageTargetAdapter> targets) where S : class
    {
        lock (s_smartPushLock)
        {
            if (source == null)
            {
                ModLogger.DebugLog($"{methodName}: Source is null, returning");
                return;
            }

            if (targets == null || targets.Count == 0)
            {
                ModLogger.DebugLog($"{methodName}: No target storages found, returning");
                return;
            }

            var state = new StorageOperationState(source.GetName(), SmartTransferOperation.Push);

            // First fill up existing partial slots as at the start of the operation
            PushSourceItemsToTarget(methodName, state, source, targets, allowPushToEmpty: false);

            // Then fill up any empty slots, and any new partial slots that are created when partially filling those empty slots
            PushSourceItemsToTarget(methodName, state, source, targets, allowPushToEmpty: true);

            ModLogger.DebugLog($"{methodName}: {state}");

            if (state.StackCount > 0)
            {
                context.ShowLocalPlayerNotification(MSG_SMART_PUSH_RESULT, state.StackCount, state.MasterStorageName, state.StorageCount);
                context.InvalidateCache();
            }

            UIRefreshHelper.ValidateAndRefreshUI(context, methodName);
        }
    }

    private static void PullSourceItemsToLoadout<T>(string methodName, StorageOperationState state, IReadOnlyList<StorageTargetAdapter> sources, StorageSourceAdapter<T> loadout) where T : class
    {
        // Loadout slots = locked, non-empty slots — derived from raw slot data.
        // Returns references to the original ItemStack objects so count mutations apply to live storage.
        var loadoutSlotData = loadout.GetSlotData();
        var loadoutSlots = ItemX.GetFilteredItems(loadoutSlotData.AllSlots, StorageFilter.LockedOnly, loadoutSlotData.LockedSlots);
        var modifiedSources = new HashSet<StorageTargetAdapter>();

        for (int i = 0; i < loadoutSlots.Length; i++)
        {
            var loadoutSlot = loadoutSlots[i];
            if (ItemX.IsEmpty(loadoutSlot))
            {
                ModLogger.DebugLog($"{methodName}: Loadout slot {i} is empty, skipping");
                continue;
            }

            int maxStackSize = ItemX.MaxStackSizeOf(loadoutSlot);
            if (maxStackSize <= 0)
            {
#if DEBUG
                ModLogger.DebugLog($"{methodName}: Loadout slot {i} in {state.MasterStorageName} has invalid max stack size {maxStackSize}, skipping");
#endif
                continue;
            }

            int itemType = ItemX.ItemTypeOf(loadoutSlot);
            if (itemType == UniqueItemTypes.EMPTY)
            {
                // This is probably redundant since an empty slot should have been caught by the IsEmpty check above, but we'll log it just in case
                ModLogger.DebugLog($"{methodName}: Loadout slot {i} has invalid item type {itemType}, skipping");
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

                if (PullToLoadoutSlots(methodName, state, loadoutSlot, source, itemType, maxStackSize, ref loadoutSlotRequiredAmount))
                {
                    modifiedSources.Add(source);
                }
            }
        }

        // Both MarkModified calls are deferred until after all iterations are complete,
        // to prevent game bag rebuilds from invalidating loadoutSlot references mid-loop
        foreach (var modifiedSource in modifiedSources)
        {
            modifiedSource.MarkModified();
        }

        if (modifiedSources.Count > 0)
        {
            loadout.MarkModified();
        }
    }

    private static void PushSourceItemsToTarget<S>(string methodName, StorageOperationState state, StorageSourceAdapter<S> source, IReadOnlyList<StorageTargetAdapter> targets, bool allowPushToEmpty) where S : class
    {
        // Pushable slots = unlocked, non-empty slots — re-read each pass so slots emptied in the
        // partial-fill pass are naturally excluded from the empty-fill pass without extra filtering.
        // Returns references to the original ItemStack objects so count mutations apply to live storage.
        var sourceSlotData = source.GetSlotData();
        var sourceSlots = ItemX.GetFilteredItems(sourceSlotData.AllSlots, StorageFilter.UnlockedOnly, sourceSlotData.LockedSlots);

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
                ModLogger.DebugLog($"{methodName}: Source slot {i} in {state.MasterStorageName} has invalid max stack size {maxStackSize}, skipping");
#endif
                continue;
            }

            int itemType = ItemX.ItemTypeOf(sourceSlot);
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

                PushToTarget(methodName, state, source, sourceSlot, target, itemType, allowPushToEmpty, maxStackSize, ref sourceSlotRemaining);
            }
        }
    }

    private static bool PullToLoadoutSlots(string methodName, StorageOperationState state, ItemStack loadoutSlot, StorageTargetAdapter source, int itemType, int maxStackSize, ref int loadoutSlotRequiredAmount)
    {
        int transferCount = 0;
        int initialStackSize = maxStackSize - loadoutSlotRequiredAmount;

        while (loadoutSlotRequiredAmount > 0)
        {
            var sourceSlot = source.GetNextPopulatedStackFor(itemType);
            if (sourceSlot == null)
            {
                // No more source slots available for this loadout slot
                break;
            }

            int sourceSlotActualCount = ItemX.CurrentStackSizeOf(sourceSlot);

            // Limit transfer to only what the loadout slot still needs
            int cappedTransferLimit = Math.Min(sourceSlotActualCount, loadoutSlotRequiredAmount);
            if (cappedTransferLimit <= 0)
            {
                // Source slot is depleted despite being in the populated map — avoid infinite loop
                break;
            }

            // Pass the capped limit so TransferTargetSlotItems doesn't transfer more than the loadout slot needs
            int tempRemaining = cappedTransferLimit;

            var transferAmount = TransferTargetSlotItems(methodName, state, sourceSlot, loadoutSlot, maxStackSize, ref tempRemaining);

            sourceSlot.count = sourceSlotActualCount - transferAmount;

            loadoutSlotRequiredAmount -= transferAmount;

            transferCount += transferAmount;

            if (transferAmount > 0)
            {
                // Reclassify source slot after transfer (might be partial now, or empty)
                source.ReclassifySlot(sourceSlot);
            }
            else
            {
                // No items transferred; source slot may be depleted — avoid infinite loop
                break;
            }
        }

        if (transferCount > 0)
        {
            int currentStackSize = maxStackSize - loadoutSlotRequiredAmount;

            // source.MarkModified() and loadout.MarkModified() are deferred to caller — see PullSourceItemsToLoadout

            state.RecordTransfer(source, loadoutSlot, initialStackSize, currentStackSize, maxStackSize, transferCount);

            return true;
        }

        return false;
    }

    private static void PushToTarget<S>(string methodName, StorageOperationState state, StorageSourceAdapter<S> source, ItemStack sourceSlot, StorageTargetAdapter target, int itemType, bool allowPushToEmpty, int maxStackSize, ref int sourceSlotRemaining) where S : class
    {
        int transferCount = 0;
        int initialStackSize = sourceSlotRemaining;

        while (sourceSlotRemaining > 0)
        {
            // Prefer partial slots; fall back to empty only if allowed
            var targetSlot = target.GetNextPartialStackFor(itemType);

            if (targetSlot == null)
            {
                if (!allowPushToEmpty)
                {
                    break;
                }

                targetSlot = target.GetNextEmptyStackFor(itemType);
                if (targetSlot == null)
                {
                    break;
                }
            }

            var transferAmount = TransferTargetSlotItems(methodName, state, sourceSlot, targetSlot, maxStackSize, ref sourceSlotRemaining);

            transferCount += transferAmount;

            if (transferAmount > 0)
            {
                target.ReclassifySlot(targetSlot);
            }
            else
            {
                // No items transferred; slot may already be full — avoid infinite loop
                break;
            }
        }

        if (transferCount > 0)
        {
            int currentStackSize = sourceSlotRemaining;

            source.MarkModified();
            target.MarkModified();

            state.RecordTransfer(target, sourceSlot, initialStackSize, currentStackSize, maxStackSize, transferCount);
        }
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
        //ModLogger.DebugLog($"{methodName}: Transferring {transferAmount} of {ItemX.NameOf(sourceSlot)} (storage: {state.MasterStorageName})");
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