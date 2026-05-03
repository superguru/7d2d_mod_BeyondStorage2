using BeyondStorage.Infrastructure;
using BeyondStorage.Storage;
using HarmonyLib;

namespace BeyondStorage.Harmony.Extends;

[HarmonyPatch(typeof(XUiC_ItemStack))]
internal static class Stack_Slot_Lock_Maintanance_Patch
{
    private static long s_callCounter = 0;
    private static readonly object s_lockObject = new();

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
}