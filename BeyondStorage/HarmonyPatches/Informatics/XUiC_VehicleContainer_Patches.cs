using BeyondStorage.Scripts.Data;
using BeyondStorage.Scripts.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_VehicleContainer))]
public class XUiC_VehicleContainer_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_VehicleContainer.UpdateLockedSlots))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void XUiC_VehicleContainer_UpdateLockedSlots_Postfix(XUiC_ContainerStandardControls _csc)
    {
        UIRefreshHelper.LogAndRefreshUI(StackOps.Stack_LockStateChange_Operation, itemStack: null, callCount: 0);
    }
}
