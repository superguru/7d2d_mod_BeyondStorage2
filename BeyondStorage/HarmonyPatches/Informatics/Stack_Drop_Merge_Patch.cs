using System.Collections.Generic;
using System.Threading;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.UI;
using HarmonyLib;

#if DEBUG
#endif

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_ItemStack))]
public class Stack_Drop_Merge_Patch
{
    private static long s_callCounter = 0;
    private static readonly Dictionary<long, SlotSnapshot> s_callHistory = [];
    private static readonly object s_lockObject = new();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandleStackSwap))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void Handle_StackSwap_Event_Prefix(XUiC_ItemStack __instance)
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
        //ModLogger.DebugLog($"{d_MethodName}: call #{callCount} for {preSnapshot.ToCompactString()}, loc={preSnapshot.SlotLocation}");
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandleStackSwap))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void Handle_StackSwap_Event_Postfix(XUiC_ItemStack __instance)
    {
        const string d_MethodName = nameof(Handle_StackSwap_Event_Postfix);

        // Capture post-execution snapshot
        var postSnapshot = new SlotSnapshot(__instance);

        SlotSnapshot preSnapshot = null;
        long callCount = s_callCounter;

        // Thread-safe retrieval of prefix snapshot
        lock (s_lockObject)
        {
            s_callHistory.TryGetValue(callCount, out preSnapshot);
        }

        // Analyze location and item type consistency when AllowDropping is true
        if (preSnapshot != null && postSnapshot != null)
        {
            AnalyzeDropConditions(d_MethodName, callCount, preSnapshot, postSnapshot, __instance);
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
            UIRefreshHelper.LogAndRefreshUI($"{StackOps.ItemStack_DropMerge_Operation}", callCount);
        }
        else
        {
#if DEBUG
            // Log inconsistent drop conditions
            //ModLogger.DebugLog($"{methodName}: call #{callCount} - isMerge={isMerge}. Inconsistent drop conditions for {preSnapshot.ToCompactString()} ➡️ {postSnapshot.ToCompactString()}");
#endif
        }
    }
}