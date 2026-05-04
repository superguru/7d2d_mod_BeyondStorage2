using BeyondStorage.Game.Recipe;
using HarmonyLib;

namespace BeyondStorage.Harmony.Extends;

[HarmonyPatch(typeof(XUiC_WorkstationOutputGrid))]
#if DEBUG
[HarmonyDebug]
#endif
internal static class XUiC_WorkstationOutputGrid_Ext
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(XUiC_WorkstationOutputGrid.UpdateData))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void XUiC_WorkstationOutputGrid_UpdateData_Postfix()
    {
        // This is called when the recipe finishes crafting on a currently opened workstation window
        WorkstationRecipe.ForegroundWorkstation_CraftCompleted();
    }
}
