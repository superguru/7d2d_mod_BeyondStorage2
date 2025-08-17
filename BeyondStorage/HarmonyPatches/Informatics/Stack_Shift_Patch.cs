using System.Collections.Generic;
using System.Threading;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.UI;

[HarmonyPatch(typeof(XUiC_ItemStack))]
public class Stack_Shift_Patch
{
    private static long s_callCounter = 0;
    private static readonly Dictionary<long, SlotSnapshot> s_callHistory = [];
    private static readonly object s_lockObject = new();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandleMoveToPreferredLocation))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void Handle_StackShift_Event_Prefix(XUiC_ItemStack __instance)
    {

        // Capture slot state snapshot
        var preSnapshot = new SlotSnapshot(__instance);

        long callCount;

        // Thread-safe update of call counter and history
        lock (s_lockObject)
        {
            callCount = Interlocked.Increment(ref s_callCounter);

            // Clear existing entry and add new one (maintain only 1 entry)
            s_callHistory.Clear();
            s_callHistory[callCount] = preSnapshot;
        }

#if DEBUG
        //ModLogger.DebugLog($"{d_MethodName}: END call #{callCount} for {preSnapshot.ToCompactString()}, loc={preSnapshot.SlotLocation}");
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandleMoveToPreferredLocation))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void Handle_StackShift_Event_Postfix(XUiC_ItemStack __instance)
    {

        // Capture post-execution snapshot
        var postSnapshot = new SlotSnapshot(__instance);

        SlotSnapshot preSnapshot = null;
        long callCount = s_callCounter;

        // Thread-safe retrieval of prefix snapshot
        lock (s_lockObject)
        {
            s_callHistory.TryGetValue(callCount, out preSnapshot);
        }

        // Handle Shift+Click logic (move stack between inventories)
        if ((preSnapshot?.IsValid ?? false) && (postSnapshot?.IsStackPresent ?? false))
        {
            // Because stacks will be merged using this method, and any overspill will be moved to the next available slot,
            // we can't reliably determine the exact slot where the stack ended up, so whether is's locked or not is not relevant.
            // All we know is that either the source or destination was maybe a locked storage slot, so we have assume that
            // a locked storage slot was involved in this operation.
            UIRefreshHelper.LogAndRefreshUI($"{StackOps.ItemStack_Shift_Operation}", callCount);
        }
    }
}