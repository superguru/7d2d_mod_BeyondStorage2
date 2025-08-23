using BeyondStorage.Scripts.Game.Recipe;
using HarmonyLib;


namespace BeyondStorage.HarmonyPatches.Recipe;

[HarmonyPatch(typeof(XUiC_WorkstationOutputGrid))]
internal static class WorkstationPatches
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