using System.Collections.Generic;
using System.Threading;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.UI;
using HarmonyLib;

#if DEBUG
using BeyondStorage.Scripts.Infrastructure;
#endif

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_ItemStack))]
public class Stack_Partial_Patch
{
    private static long s_callCounter = 0;
    private static readonly Dictionary<long, SlotSnapshot> s_callHistory = [];
    private static readonly object s_lockObject = new();

    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_ItemStack.HandlePartialStackPickup))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void Handle_PartialStackPickup_Event_Prefix(XUiC_ItemStack __instance)
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

        // Thread-safe update of call counter and history
        lock (s_lockObject)
        {
            callCount = Interlocked.Increment(ref s_callCounter);

            // Clear existing entry and add new one (maintain only 1 entry)
            s_callHistory.Clear();
            s_callHistory[callCount] = preSnapshot;
        }

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
    public static void Handle_PartialStackPickup_Event_Postfix(XUiC_ItemStack __instance)
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
}