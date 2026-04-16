using BeyondStorage.Source.Data;
using BeyondStorage.Source.Infrastructure;

namespace BeyondStorage.HarmonyPatches.Informatics;

//[HarmonyPatch(typeof(XUiC_ItemStack))]
internal static class Stack_Slot_Lock_Maintanance_Patch
{
    private static long s_callCounter = 0;
    private static readonly object s_lockObject = new();

    //    [HarmonyPrefix]
    //    [HarmonyPatch(nameof(XUiC_ItemStack.UserLockedSlot), MethodType.Setter)]
    //#if DEBUG
    //    [HarmonyDebug]
    //#endif
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

        var slotNumber = __instance.SlotNumber;
        ModLogger.DebugLog($"{d_MethodName}: UserLockedSlot({slotNumber}, {value}) setter called for {ItemX.Info(stack)}, call count: {s_callCounter}");
    }
}