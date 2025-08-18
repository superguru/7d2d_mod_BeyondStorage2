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
        const string d_MethodName = nameof(XUiC_VehicleContainer_UpdateLockedSlots_Postfix);

        UIRefreshHelper.LogAndRefreshUI(d_MethodName, 0);
    }
}
