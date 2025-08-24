using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Caching;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Harmony;
using HarmonyLib;
using XMLData.Item;

namespace BeyondStorage.HarmonyPatches.Functions;

[HarmonyPatch(typeof(ItemActionAttack))]
internal static class Item_Action_Attack_Patches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionAttack.SetupRadial), [typeof(XUiC_Radial), typeof(EntityPlayerLocal)])]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionAttack_SetupRadial_Patch(IEnumerable<CodeInstruction> originalInstructions, ILGenerator generator)
    {
        var targetMethodString = $"{typeof(ItemActionAttack)}.{nameof(ItemActionAttack.SetupRadial)}";

        LocalBuilder local_ammoId = generator.DeclareLocal(typeof(int));

        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ItemData), nameof(ItemData.Id))), // get_Id()
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.GetItemCount), [typeof(int)])),
            new CodeInstruction(OpCodes.Stloc_S, 4),                        // itemCount
        };

        // Create replacement instructions
        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldloc_S, local_ammoId.LocalIndex),  // local_ammoId
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemPropertiesCache), nameof(ItemPropertiesCache.CreateTemporaryItemValue))),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemCommon), nameof(ItemCommon.GetStorageItemCount))),
            new CodeInstruction(OpCodes.Ldloc_S, 4),                        // load itemCount
            new CodeInstruction(OpCodes.Add),                               // add storage count to player inventory count
            new CodeInstruction(OpCodes.Stloc_S, 4),                        // store result in itemCount
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 3,
            IsInsertMode = true,
            MaxPatches = 1,
            MinimumSafetyOffset = 5,
            ExtraLogging = false,
        };

        var response = ILPatchEngine.ApplyPatches(request);

        if (response.IsPatched)
        {
            var patchIdx = response.OriginalPositions[0];  // this should be the get_ID() call
            patchIdx += 1; // move to the next instruction, which is where we want to store the ammoId
            request.NewInstructions.InsertRange(patchIdx, [
                new CodeInstruction(OpCodes.Stloc_S, local_ammoId.LocalIndex),  // store the ammoId
                new CodeInstruction(OpCodes.Ldloc_S, local_ammoId.LocalIndex),  // load it back onto the stack
                // the original code will now call XUiM_PlayerInventory.GetItemCount with the ammoId from local_ammoId
            ]);
        }


        return response.BestInstructions(request);
    }
}