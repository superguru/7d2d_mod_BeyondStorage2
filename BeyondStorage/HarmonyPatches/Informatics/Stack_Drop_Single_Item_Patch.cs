using System.Threading;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.UI;

[HarmonyPatch(typeof(XUiC_ItemStack))]
public class Stack_Drop_Single_Item_Patch
{
    private static long s_callCounter = 0;
    private static readonly object s_lockObject = new();

    // Logging format constants
    private const int MN_FMT = -45;  // Method Name Format Length
    private const int CN_FMT = 3;    // Call Number Format Length

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandleSlotChangeEvent))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void Handle_DropSingle_Event_Prefix(XUiC_ItemStack __instance)
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

        // Early validation - check if slot has content
        if (preSnapshot.IsNullInstance)
        {
            return;
        }

        if (preSnapshot.IsEmpty)
        {
            return;
        }

        // Analyze drag and drop system state using the new analyzer
        var dragDropAnalyzer = new DragDropAnalyzer(preSnapshot, __instance);

        // Predict the expected operation type
        var operation = SwapOperationStateMachine.GetPredictedSwapAction(preSnapshot, dragDropAnalyzer.IsDragEmpty, dragDropAnalyzer.DragStackInfo, dragDropAnalyzer.DragPickupLocation);

        if (operation != SwapAction.SwapOrMergeOperation)
        {
#if DEBUG
            //ModLogger.DebugLog($"{d_MethodName,MN_FMT}: call #{callCount,CN_FMT} skip different op, operation {operation}, pre {preSnapshot}");
#endif
            return;

        }

        if (!preSnapshot.IsDragAndDrop)
        {
            //TODO:refresh
            UIRefreshHelper.LogAndRefreshUI($"{StackOps.ItemStack_DropSingleItem_Operation}", callCount);
            return;
        }
#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName,MN_FMT}: END call #{callCount,CN_FMT}, operation {operation}, pre {preSnapshot}, dragInfo {dragDropAnalyzer}");
#endif
    }
}