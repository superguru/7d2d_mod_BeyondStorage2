using System.Threading;
using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Diagnostics;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_ItemStack))]
public class Find_ItemStack_Setter_Patch
{
    private static long s_callCounter = 0;

    //    [HarmonyPrefix]
    //    [HarmonyPatch(nameof(XUiC_ItemStack.ItemStack), MethodType.Setter)]
    //#if DEBUG
    //    [HarmonyDebug]
    //#endif
    public static void XUiC_ItemStack_ItemStack_Setter_Prefix(XUiC_ItemStack __instance, ItemStack value)
    {
        const string d_MethodName = nameof(XUiC_ItemStack_ItemStack_Setter_Prefix);
        var callCount = Interlocked.Increment(ref s_callCounter);

        // Cache information
        var currStack = __instance.ItemStack;
        string currItemDescr = ItemX.Info(currStack);

        var newStack = value;
        string newItemDescr = ItemX.Info(newStack);

        bool currStackPresent = ItemX.IsStackPresent(currStack);
        bool newStackPresent = ItemX.IsStackPresent(newStack);

        var caseStr = C(currStackPresent, newStackPresent);

        // Cache slot information
        var slotNumber = __instance.SlotNumber;
        bool isSlotLocked = ItemX.IsSlotLocked(__instance);
        var slotLocation = __instance.StackLocation;
        bool isSlotPlayerInventory = ItemX.IsPlayerInventory(slotLocation);
        string inventoryName = ItemX.GetInventoryName(isSlotPlayerInventory);
        bool isDragAndDrop = __instance.IsDragAndDrop;

        // Log this call, whatever the CASE may be
        //ModLogger.DebugLog($"{d_MethodName} [{callCount}]: EVENT {caseStr} {P(currStackPresent)} {P(newStackPresent)}, curr:{currItemDescr}, slot:{slotNumber}{L(isSlotLocked)}@{inventoryName}, new:{newItemDescr}");

        // CASE_0 0 0 : CLEAR_CLEAR_NOP -> Early exit if no meaningful change
        //if ((!currStackPresent && !newStackPresent) || ItemX.EqualContents(newStack, currStack))
        if (!currStackPresent && !newStackPresent)
        {
            ModLogger.DebugLog($"{d_MethodName} [{callCount}]: {caseStr} {ItemX.P(currStackPresent)} {ItemX.P(newStackPresent)}, new:{newItemDescr}, curr:{currItemDescr}, slot:{slotNumber}{ItemX.L(isSlotLocked)}@{inventoryName}");
            return;
        }

        // CASE_1 0 1: PUT_INTO_EMPTY
        if (!currStackPresent && newStackPresent)
        {
            ModLogger.DebugLog($"{d_MethodName} [{callCount}]: {caseStr} {ItemX.P(currStackPresent)} {ItemX.P(newStackPresent)} MOVE START NOT FOUND for new:{SlotSig(newItemDescr, slotNumber, isSlotLocked, inventoryName, isDragAndDrop)}");
            //if (newItemDescr == "resourceWood:59")
            //{
            //    var message = $"PUT_INTO_EMPTY for new:{newItemDescr}, existing:{moveInfo.ItemDescr}";
            //    ModLogger.DebugLog(StackTraceProvider.AppendStackTrace(message));
            //}

            return;
        }

        // CASE_2 1 0: CLEAR_EXISTING
        if (currStackPresent && !newStackPresent)
        {
            ModLogger.DebugLog($"{d_MethodName} [{callCount}]: {caseStr} {ItemX.P(currStackPresent)} {ItemX.P(newStackPresent)} MOVE STARTS for curr:{SlotSig(currItemDescr, slotNumber, isSlotLocked, inventoryName, isDragAndDrop)}");

            return;
        }

        // CASE_3 1 1 : BOTH_FILLED_TBD -> Not sure what to do yet
        if (currStackPresent && newStackPresent)
        {
            ModLogger.DebugLog($"{d_MethodName} [{callCount}]: {caseStr} XXXXXXX_001, newItemDescr='{newItemDescr}'");
            ModLogger.DebugLog($"{d_MethodName} [{callCount}]: {caseStr} {ItemX.P(currStackPresent)} {ItemX.P(newStackPresent)}, new:{newItemDescr}, curr:{currItemDescr}, slot:{slotNumber}{ItemX.L(isSlotLocked)}@{inventoryName}");
            //Find the stack trace
            if (newItemDescr == "resourceWood:1")
            {
                ModLogger.DebugLog($"{d_MethodName} [{callCount}]: {caseStr} {ItemX.P(currStackPresent)} {ItemX.P(newStackPresent)} MERGE_STACKS? for curr:{currItemDescr}");
                var message = $"MERGE_STACKS? for curr:{currItemDescr}";
                ModLogger.DebugLog(StackTraceProvider.AppendStackTrace(message));
            }
            ModLogger.DebugLog($"{d_MethodName} [{callCount}]: {caseStr} XXXXXXX_002");
            return;
        }

        ModLogger.DebugLog($"{d_MethodName} [{callCount}]: {caseStr} {ItemX.P(currStackPresent)} {ItemX.P(newStackPresent)} UNHANDLED curr:{SlotSig(currItemDescr, slotNumber, isSlotLocked, inventoryName, isDragAndDrop)}, new:{newItemDescr}, same={newItemDescr == currItemDescr}");
    }

    // Helper methods
    static string CT(int caseNum)
    {
        switch (caseNum)
        {
            case 0: return "CLEAR_CLEAR_NOP";  // Both are empty
            case 1: return "PUT_INTO_EMPTY";   // Current is empty, New is filled
            case 2: return "CLEAR_EXISTING";   // Current is filled, New is empty
            case 3: return "BOTH_FILLED_TBD";  // Both are filled, but we don't know what to do yet
            default:
                return "WTF";
        }
    }

    static string C(bool currPresent, bool newPresent)
    {
        int currPresentInt = currPresent.Ordinal();
        int newPresentInt = newPresent.Ordinal();

        int caseNum = (currPresentInt << 1) | newPresentInt;
        return $"CASE_{caseNum}({currPresentInt},{newPresentInt})->{CT(caseNum)}";
    }

    static string SlotSig(string itemDescr, int slotNumber, bool isSlotLocked, string inventoryName, bool isDragAndDrop)
    {
        return $"{itemDescr} in slot:{slotNumber}{ItemX.L(isSlotLocked)}@{inventoryName} (isDragAndDrop={isDragAndDrop})";
    }
}