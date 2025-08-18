using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_CollectedItemList))]
public static class XUiC_CollectedItemList_StorageIntegration_Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(XUiC_CollectedItemList.AddItemStack))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static bool Intercept_AddItemStack_Prefix(XUiC_CollectedItemList __instance, ItemStack _is, bool _bAddOnlyIfNotExisting)
    {
#if DEBUG
        const string d_MethodName = nameof(Intercept_AddItemStack_Prefix);
#endif
        var itemInfo = ItemX.Info(_is);

        var isStackShiftIntercept = Stack_Shift_Patch.StackMatchesCurrentOp(_is);
        if (isStackShiftIntercept)
        {
#if DEBUG
            ModLogger.DebugLog($"{d_MethodName}: Skipping AddItemStack (notification) for matching stack ({itemInfo}) shift operation");
#endif
            return false; // Skip original method execution
        }

        ModLogger.DebugLog($"{d_MethodName}: Proceeding with AddItemStack for stack ({itemInfo})");
        return true; // Proceed with original method execution
    }
}