using BeyondStorage.Game.Recipe;
using HarmonyLib;

namespace BeyondStorage.Harmony.Extends;

[HarmonyPatch(typeof(TileEntityWorkstation))]
#if DEBUG
[HarmonyDebug]
#endif
internal static class TileEntityWorkstation_Ext
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(TileEntityWorkstation.AddCraftComplete))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static void TileEntityWorkstation_AddCraftComplete_Prefix()
    {
        // This is called when the recipe finishes crafting on a workstation TE that is NOT open on a player screen
        WorkstationRecipe.BackgroundWorkstation_CraftCompleted();
    }
}
