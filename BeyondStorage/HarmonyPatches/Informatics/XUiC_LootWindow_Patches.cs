using BeyondStorage.Scripts.UI;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Informatics;

[HarmonyPatch(typeof(XUiC_LootWindow))]
public class XUiC_LootWindow_Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_LootWindow.UpdateLockedSlots))]
#if DEBUG
    [HarmonyDebug]
#endif
    public static void XUiC_LootWindow_UpdateLockedSlots_Postfix(XUiC_ContainerStandardControls _csc)
    {
        const string d_MethodName = nameof(XUiC_LootWindow_UpdateLockedSlots_Postfix);

        UIRefreshHelper.LogAndRefreshUI(d_MethodName);
    }
}
