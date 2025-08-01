using System.Collections.Generic;
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
    private static IEnumerable<CodeInstruction> TileEntityWorkstation_AddCraftComplete_Patch(IEnumerable<CodeInstruction> originalInstructions)
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

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 0,     // Insert at the match position
            IsInsertMode = true,       // Insert new instructions at the pattern
            MaxPatches = 1,
            MinimumSafetyOffset = 0,   // No special safety requirements
            ExtraLogging = false
        };

        var patchResult = ILPatchEngine.ApplyPatches(request);

        if (patchResult.IsPatched)
        {
            return request.NewInstructions;
        }

        var response = ILPatchEngine.ApplyPatches(request);
        return response.BestInstructions(request);
    }
}