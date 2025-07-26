using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BeyondStorage.Scripts.ContainerLogic.Recipe;
using BeyondStorage.Scripts.Utils;
using HarmonyLib;


namespace BeyondStorage.Recipe;

[HarmonyPatch(typeof(TileEntityWorkstation))]
public class WorkstationRecipePatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(TileEntityWorkstation.AddCraftComplete))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> TileEntityWorkstation_AddCraftComplete_Patch(IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(TileEntityWorkstation)}.{nameof(TileEntityWorkstation.AddCraftComplete)}";

        // Create search pattern for Ldarg_0 instruction
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldarg_0)
        };

        // Create replacement instructions (insert at the found pattern position)
        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WorkstationRecipe), nameof(WorkstationRecipe.BackgroundWorkstation_CraftCompleted)))
        };

        var patchRequest = new PatchUtil.PatchRequest
        {
            OriginalInstructions = instructions.ToList(),
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 0,     // Insert at the match position
            IsInsertMode = true,       // Insert new instructions at the pattern
            MaxPatches = 1,
            MinimumSafetyOffset = 0,   // No special safety requirements
            ExtraLogging = true        // Enable extra logging for debugging
        };

        var patchResult = PatchUtil.ApplyPatches(patchRequest);

        if (patchResult.IsPatched)
        {
            return patchRequest.NewInstructions;
        }

        var response = PatchUtil.ApplyPatches(patchRequest);
        return response.BestInstructions(patchRequest);
    }
}