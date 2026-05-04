using System.Threading;
using BeyondStorage.Data;
using BeyondStorage.Infrastructure;
using BeyondStorage.Storage;
using BeyondStorage.UI;
using HarmonyLib;

namespace BeyondStorage.Harmony.Extends;

[HarmonyPatch(typeof(XUiC_ItemStack))]
internal static class XUiC_ItemStack_Ext
{
    private static long s_callCounter = 0;
    private static SlotSnapshot s_currentSnapshot = null;
    private static readonly object s_lockObject = new();

    private static void SingleDropEvent(XUiC_ItemStack __instance)
    {
        // Increment call counter immediately at the start to ensure logging consistency
        long callCount;
        lock (s_lockObject)
        {
            callCount = Interlocked.Increment(ref s_callCounter);
        }

        // Capture slot state snapshot
#pragma warning disable IDE0017 // Simplify object initialization
        var preSnapshot = new SlotSnapshot(__instance);
#pragma warning restore IDE0017 // Simplify object initialization
        preSnapshot.OriginalCallCount = callCount;
        bool lastClicked = __instance.lastClicked;

        // Early validation - check if slot has content
        if (preSnapshot.IsNullInstance)
        {
            return;
        }

        if (preSnapshot.IsEmpty)
        {
            if (!lastClicked)
            {
                return;
            }
        }
        else
        {
            // Analyze drag and drop system state using the new analyzer
            var dragDropAnalyzer = new DragDropAnalyzer(preSnapshot, __instance);

            // Predict the expected operation type
            preSnapshot.PredictedOperation = SwapOperationStateMachine.GetPredictedSwapAction(preSnapshot, dragDropAnalyzer.IsDragEmpty, dragDropAnalyzer.DragStackInfo, dragDropAnalyzer.DragPickupLocation);

            if (preSnapshot.PredictedOperation != SwapAction.SwapOrMergeOperation)
            {
                return;
            }

            if (preSnapshot.IsDragAndDrop)
            {
                return;
            }
        }

        UIRefreshHelper.LogAndRefreshUI(StackOps.ItemStack_DropSingleItem_Operation, __instance, callCount);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandleSlotChangeEvent))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void Handle_DropSingle_Event_Prefix(XUiC_ItemStack __instance)
    {
        SingleDropEvent(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandleDropOne))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool Handle_DropOne_Prefix(XUiC_ItemStack __instance)
    {
        SingleDropEvent(__instance);

        return true; // Still continue with the original method
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.UserLockedSlot), MethodType.Setter)]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void Handle_UserLockedSlot_Setter_Prefix(XUiC_ItemStack __instance, bool value)
    {
        const string d_MethodName = nameof(Handle_UserLockedSlot_Setter_Prefix);

        lock (s_lockObject)
        {
            s_callCounter++;
        }

        if (__instance == null)
        {
            // Can this really happen? Just in case, log it and return.
            ModLogger.DebugLog($"{d_MethodName}: __instance is null, call count: {s_callCounter}");
            return;
        }

        if (value == __instance.UserLockedSlot)
        {
            return;
        }

        var stack = __instance.ItemStack;
        if (stack == null || stack.count == 0)
        {
            return;
        }

        // A locked slot change affects which slots are pushable — invalidate the cached context
        // so the next operation rebuilds slot maps with the updated lock state
        StorageContextFactory.InvalidateContext();
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandlePartialStackPickup))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void Handle_PartialStackPickup_Event_Prefix(XUiC_ItemStack __instance, ref SlotSnapshot __state)
    {
#if DEBUG
        const string d_MethodName = nameof(Handle_PartialStackPickup_Event_Prefix);
#endif

        if (__instance?.xui?.dragAndDrop == null)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: call skipped for null instance or drag and drop system");
#endif
            return;
        }

        ItemStack currentStack = __instance.xui.dragAndDrop.CurrentStack;
        if (currentStack == null)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: call skipped for null current stack");
#endif
            return;
        }

        if (__instance.itemStack == null)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: call skipped for null item stack");
#endif
            return;
        }

        bool validPartialPickup = currentStack.IsEmpty() && !__instance.itemStack.IsEmpty();
        if (!validPartialPickup)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: call skipped for invalid partial pickup (current stack empty, item stack not empty)");
#endif
            return;
        }

        // Capture slot state snapshot
        var preSnapshot = new SlotSnapshot(__instance);

        long callCount;

        // Thread-safe update of call counter
        lock (s_lockObject)
        {
            callCount = Interlocked.Increment(ref s_callCounter);
        }

        preSnapshot.OriginalCallCount = callCount;

        // Store snapshot in per-call state for the postfix
        __state = preSnapshot;

        if (preSnapshot.IsStorageInventory)
        {
            // Need to refresh UI if this is a storage inventory. The game already does this for player inventory.                
            UIRefreshHelper.LogAndRefreshUI(StackOps.ItemStack_Pickup_Half_Stack_Operation, __instance, callCount);
        }

        //ModLogger.DebugLog($"{d_MethodName}: END call #{callCount} for {preSnapshot}");
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandlePartialStackPickup))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void Handle_PartialStackPickup_Event_Postfix(XUiC_ItemStack __instance, SlotSnapshot __state)
    {
        // Capture post-execution snapshot
        var postSnapshot = new SlotSnapshot(__instance);

        // Retrieve prefix snapshot for comparison
        SlotSnapshot preSnapshot = __state;
        long callCount = s_callCounter;

        string changeInfo = "No_Pre_Snap";
        if (preSnapshot != null)
        {
            // Compare before and after snapshots
            bool stackChanged = preSnapshot.ItemDescription != postSnapshot.ItemDescription;
            bool presenceChanged = preSnapshot.IsStackPresent != postSnapshot.IsStackPresent;

            if (stackChanged || presenceChanged)
            {
                changeInfo = $" [CHANGED: {preSnapshot.ItemDescription} → {postSnapshot.ItemDescription}]";
            }
            else
            {
                changeInfo = " [NO_CHANGE]";
            }
        }
#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: END call #{callCount} for {postSnapshot.ToCompactString()}{changeInfo}");
#endif
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandleMoveToPreferredLocation))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void Handle_StackShift_Event_Prefix(XUiC_ItemStack __instance)
    {
#if DEBUG
        //const string d_MethodName = nameof(Handle_StackShift_Event_Prefix);
#endif
        // Capture slot state snapshot
        var preSnapshot = new SlotSnapshot(__instance);

        long callCount;

        // Thread-safe update of call counter and history
        lock (s_lockObject)
        {
            callCount = Interlocked.Increment(ref s_callCounter);
            preSnapshot.OriginalCallCount = callCount;
            s_currentSnapshot = preSnapshot;
        }

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: call #{callCount} - detected shift operation, pre {preSnapshot}");
#endif

        // Only refresh UI for storage inventory operations
        if (preSnapshot.IsStorageInventory)
        {
            UIRefreshHelper.LogAndRefreshUI(StackOps.ItemStack_Shift_Operation, __instance, callCount);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandleMoveToPreferredLocation))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void Handle_StackShift_Event_Postfix(XUiC_ItemStack __instance)
    {
#if DEBUG
        //const string d_MethodName = nameof(Handle_StackShift_Event_Postfix);
#endif
        // Capture post-execution snapshot
        var postSnapshot = new SlotSnapshot(__instance);

        SlotSnapshot preSnapshot = null;
        long callCount;

        // Thread-safe retrieval of prefix snapshot
        lock (s_lockObject)
        {
            preSnapshot = s_currentSnapshot;
            callCount = s_callCounter;
            s_currentSnapshot = null; // Clear current snapshot after use
        }

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: END call #{callCount} for {preSnapshot?.ToString() ?? "No_Pre_Snap"} ➡️ {postSnapshot}");
#endif

        // Handle Shift+Click logic (move stack between inventories)
        if (preSnapshot?.IsValid ?? false)
        {
            // Because stacks will be merged using this method, and any overspill will be moved to the next available slot,
            // we can't reliably determine the exact slot where the stack ended up, so whether is's locked or not is not relevant.
            // All we know is that either the source or destination was maybe a locked storage slot, so we have assume that
            // a locked storage slot was involved in this operation.
            UIRefreshHelper.LogAndRefreshUI(StackOps.ItemStack_Shift_Operation, __instance, callCount);
        }
    }

    public static bool StackMatchesCurrentOp(ItemStack stack)
    {
        lock (s_lockObject)
        {
            var currentSnapshot = s_currentSnapshot;
            if (currentSnapshot == null)
            {
                return false; // No valid snapshot to compare against
            }

            return currentSnapshot.EqualContents(stack);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandleStackSwap))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void Handle_StackSwap_Event_Prefix(XUiC_ItemStack __instance, ref SlotSnapshot __state)
    {
        // Capture slot state snapshot
        var preSnapshot = new SlotSnapshot(__instance);

        long callCount;

        // Thread-safe update of call counter
        lock (s_lockObject)
        {
            callCount = Interlocked.Increment(ref s_callCounter);
        }

        preSnapshot.OriginalCallCount = callCount;

        // Store snapshot in per-call state for the postfix
        __state = preSnapshot;

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: call #{callCount} for {preSnapshot.ToCompactString()}, loc={preSnapshot.SlotLocation}");
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandleStackSwap))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void Handle_StackSwap_Event_Postfix(XUiC_ItemStack __instance, SlotSnapshot __state)
    {
        const string d_MethodName = nameof(Handle_StackSwap_Event_Postfix);

        // Capture post-execution snapshot
        var postSnapshot = new SlotSnapshot(__instance);

        // Retrieve prefix snapshot for comparison
        SlotSnapshot preSnapshot = __state;

        // Analyze location and item type consistency when AllowDropping is true
        if (preSnapshot != null && postSnapshot != null)
        {
            AnalyzeDropConditions(d_MethodName, s_callCounter, preSnapshot, postSnapshot, __instance);
        }
    }

    /// <summary>
    /// Analyzes drop conditions, location consistency, and item type matching when AllowDropping is true
    /// </summary>
    private static void AnalyzeDropConditions(string methodName, long callCount, SlotSnapshot preSnapshot, SlotSnapshot postSnapshot, XUiC_ItemStack instance)
    {
        // Check if AllowDropping is true for either snapshot
        bool allowDropPre = preSnapshot.AllowDropping;
        bool allowDropPost = postSnapshot.AllowDropping;

        bool isMerge = false;
        if (allowDropPre && allowDropPost)
        {
            // Check location consistency
            var locationPre = preSnapshot.SlotLocation;
            var locationPost = postSnapshot.SlotLocation;

            if (locationPre == locationPost)
            {
                isMerge = true; // Same location, can merge stacks

                // Add future conditions here if needed
            }
        }

        if (isMerge)
        {
#if DEBUG
            //var operation = SwapAction.SwapOrMergeOperation;
            //ModLogger.DebugLog($"{methodName}: call #{callCount} - {operation} for {preSnapshot.ToCompactString()} ➡️ {postSnapshot.ToCompactString()}");
#endif
            // Because stacks will be merged using this method, and any overspill will be moved to the next available slot,
            // we can't reliably determine the exact slot where the stack ended up, so whether is's locked or not is not relevant.
            // All we know is that either the source or destination was maybe a locked storage slot, so we have assume that
            // a locked storage slot was involved in this operation.
            UIRefreshHelper.LogAndRefreshUI(StackOps.ItemStack_DropMerge_Operation, instance, callCount);
        }
        else
        {
#if DEBUG
            // Log inconsistent drop conditions
            //ModLogger.DebugLog($"{methodName}: call #{callCount} - isMerge={isMerge}. Inconsistent drop conditions for {preSnapshot.ToCompactString()} ➡️ {postSnapshot.ToCompactString()}");
#endif
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.SwapItem))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void Handle_Pickup_DropStack_Event_Prefix(XUiC_ItemStack __instance, ref SlotSnapshot __state)
    {
        // Increment call counter immediately at the start to ensure logging consistency
        long callCount;
        lock (s_lockObject)
        {
            callCount = Interlocked.Increment(ref s_callCounter);
        }

#if DEBUG
        //const string d_MethodName = nameof(Handle_Pickup_DropStack_Event_Prefix);
        //ModLogger.DebugLog($"{d_MethodName}: call #{callCount} STARTED - analyzing swap operation preconditions");
#endif

        // Capture slot state snapshot
#pragma warning disable IDE0017 // Simplify object initialization
        var preSnapshot = new SlotSnapshot(__instance);
#pragma warning restore IDE0017 // Simplify object initialization
        preSnapshot.OriginalCallCount = callCount;

        // Early validation - check if slot has content
        if (preSnapshot.IsNullInstance)
        {
            return;
        }

        // Analyze drag and drop system state using the new analyzer
        var dragDropAnalyzer = new DragDropAnalyzer(preSnapshot, __instance);

        // Predict the expected operation type
        preSnapshot.PredictedOperation = SwapOperationStateMachine.GetPredictedSwapAction(preSnapshot, dragDropAnalyzer.IsDragEmpty, dragDropAnalyzer.DragStackInfo, dragDropAnalyzer.DragPickupLocation);

        // Check for early exit conditions
        if (!preSnapshot.IsStackPresent)
        {
            preSnapshot.IsValid = preSnapshot.IsValid || dragDropAnalyzer.CanSwap;
            return;
        }

        if (!preSnapshot.IsStackPresent && dragDropAnalyzer.IsDragEmpty)
        {
            // Both target slot and drag stack are empty - no operation possible
            return;
        }

        // Store snapshot in per-call state for the postfix
        __state = preSnapshot;

        if (preSnapshot.IsValid && preSnapshot.IsStorageInventory && preSnapshot.PredictedOperation == SwapAction.PickupFromSource)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName}: call #{callCount} - detected storage inventory pickup operation, pre {preSnapshot}, dragInfo {dragDropAnalyzer}");
#endif
            // Need to refresh UI if this is a storage inventory. The game already does this for player inventory.                
            UIRefreshHelper.LogAndRefreshUI(StackOps.ItemStack_Pickup_Operation, __instance, callCount);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_ItemStack.SwapItem))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void Handle_Pickup_DropStack_Event_Postfix(XUiC_ItemStack __instance, SlotSnapshot __state)
    {
        long callCount = s_callCounter;
#if DEBUG
        //const string d_MethodName = nameof(Handle_Pickup_DropStack_Event_Postfix);
        //ModLogger.DebugLog($"{d_MethodName}: call #{callCount} STARTED - analyzing swap operation results");
#endif
        // Capture post-execution snapshot
        var postSnapshot = new SlotSnapshot(__instance);

        // Retrieve prefix snapshot for comparison
        SlotSnapshot preSnapshot = __state;

        if (preSnapshot == null)
        {
            return;
        }

        // Analyze the changes that occurred
        if (preSnapshot.IsValid)
        {
            // Determine swap operation type
            SwapAction operation = SwapOperationStateMachine.GetActualSwapAction(preSnapshot, postSnapshot);

            if (postSnapshot.IsStorageInventory && (operation == SwapAction.SwapSameItem || operation == SwapAction.SwapDifferentItems || preSnapshot.PredictedOperation == SwapAction.PickupFromSource))
            {
#if DEBUG
                //ModLogger.DebugLog($"{d_MethodName}: call #{callCount} - detected storage inventory swap operation, pre {preSnapshot} predicted_op {preSnapshot.PredictedOperation}, post {postSnapshot} operation {operation}");
#endif
                // Need to refresh UI if this is a storage inventory. The game already does this for player inventory.
                UIRefreshHelper.LogAndRefreshUI(StackOps.ItemStack_Drop_Operation, __instance, callCount);
            }
        }
    }
}
