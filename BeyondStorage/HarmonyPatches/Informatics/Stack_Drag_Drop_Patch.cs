using System.Threading;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_ItemStack))]
public class Stack_Drag_Drop_Patch
{
    private static long s_callCounter = 0;
    private static SlotSnapshot s_callHistory = null;
    private static readonly object s_lockObject = new();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.SwapItem))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void Handle_Pickup_DropStack_Event_Prefix(XUiC_ItemStack __instance)
    {
        // Increment call counter immediately at the start to ensure logging consistency
        long callCount;
        lock (s_lockObject)
        {
            callCount = Interlocked.Increment(ref s_callCounter);
        }

#if DEBUG
        const string d_MethodName = nameof(Handle_Pickup_DropStack_Event_Prefix);
        ModLogger.DebugLog($"{d_MethodName}: call #{callCount} STARTED - analyzing swap operation preconditions");
#endif

        // Capture slot state snapshot
        var preSnapshot = new SlotSnapshot(__instance);
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

        // Store snapshot for successful execution path
        lock (s_lockObject)
        {
            s_callHistory = preSnapshot;
            preSnapshot.OriginalCallCount = callCount;
        }

        if (preSnapshot.IsValid && preSnapshot.IsStorageInventory && preSnapshot.PredictedOperation == SwapAction.PickupFromSource)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: call #{callCount} - detected storage inventory pickup operation, pre {preSnapshot}, dragInfo {dragDropAnalyzer}");
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
    public static void Handle_Pickup_DropStack_Event_Postfix(XUiC_ItemStack __instance)
    {
        long callCount = s_callCounter;
#if DEBUG
        const string d_MethodName = nameof(Handle_Pickup_DropStack_Event_Postfix);
        //ModLogger.DebugLog($"{d_MethodName}: call #{callCount} STARTED - analyzing swap operation results");
#endif
        // Capture post-execution snapshot
        var postSnapshot = new SlotSnapshot(__instance);

        // Retrieve prefix snapshot for comparison
        SlotSnapshot preSnapshot = null;
        lock (s_lockObject)
        {
            preSnapshot = s_callHistory;
        }

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
                ModLogger.DebugLog($"{d_MethodName}: call #{callCount} - detected storage inventory swap operation, pre {preSnapshot} predicted_op {preSnapshot.PredictedOperation}, post {postSnapshot} operation {operation}");
#endif
                // Need to refresh UI if this is a storage inventory. The game already does this for player inventory.
                UIRefreshHelper.LogAndRefreshUI(StackOps.ItemStack_Drop_Operation, __instance, callCount);
            }
        }
    }
}
