using System.Threading;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using BeyondStorage.Scripts.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_ItemStack))]
internal static class Stack_Shift_Patch
{
    private static long s_callCounter = 0;
    private static SlotSnapshot s_currentSnapshot = null;
    private static readonly object s_lockObject = new();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandleMoveToPreferredLocation))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void Handle_StackShift_Event_Prefix(XUiC_ItemStack __instance)
    {
#if DEBUG
        const string d_MethodName = nameof(Handle_StackShift_Event_Prefix);
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
        ModLogger.DebugLog($"{d_MethodName}: call #{callCount} - detected shift operation, pre {preSnapshot}");
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
        const string d_MethodName = nameof(Handle_StackShift_Event_Postfix);
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
        ModLogger.DebugLog($"{d_MethodName}: END call #{callCount} for {preSnapshot?.ToString() ?? "No_Pre_Snap"} ➡️ {postSnapshot}");
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
}