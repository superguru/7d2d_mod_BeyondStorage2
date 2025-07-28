using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Utils;
using HarmonyLib;

namespace BeyondStorage.Scripts.ContainerLogic.Item;

[HarmonyPatch(typeof(ItemActionTextureBlock))]
public class ItemActionTextureBlockPatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionTextureBlock.checkAmmo))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionTextureBlock_checkAmmo_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodString = $"{typeof(ItemActionTextureBlock)}.{nameof(ItemActionTextureBlock.checkAmmo)}";

        // Create search pattern for GetAllItemStacks method call
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldc_I4_1),
            new CodeInstruction(OpCodes.Ret)
        };

        // Create replacement instructions
        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_0), // this (ItemActionTextureBlock)
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemActionTextureBlock), "currentMagazineItem")),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemTexture), nameof(ItemTexture.ItemTexture_XXX))),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Cgt),
            new CodeInstruction(OpCodes.Ret)
        };

        var patchRequest = new PatchUtil.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 0,
            IsInsertMode = true,
            MaxPatches = 1,
            MinimumSafetyOffset = 0,
            ExtraLogging = true
        };

        var response = PatchUtil.ApplyPatches(patchRequest);
        return response.BestInstructions(patchRequest);
    }
}