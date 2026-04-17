using System.Threading;
using BeyondStorage.Source.Data;
using BeyondStorage.Source.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_ItemStack))]
internal static class Stack_Drop_Single_Item_Patch
{
    private static long s_callCounter = 0;
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
        var preSnapshot = new SlotSnapshot(__instance);
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
        const string d_MethodName = nameof(Handle_DropSingle_Event_Prefix);

        SingleDropEvent(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandleDropOne))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static bool Handle_DropOne_Prefix(XUiC_ItemStack __instance)
    {
        const string d_MethodName = nameof(Handle_DropOne_Prefix);

        SingleDropEvent(__instance);

        return true; // Still continue with the original method
    }
}