using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Harmony;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Functions;

[HarmonyPatch(typeof(BlockSecureLootSigned))]
internal static class BlockSecureLootSigned_Lockpick_Patches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(BlockSecureLootSigned.OnBlockActivated), [typeof(string), typeof(WorldBase), typeof(int), typeof(Vector3i), typeof(BlockValue), typeof(EntityPlayerLocal)])]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> BlockSecureLootSigned_OnBlockActivated_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodString = $"{typeof(BlockSecureLootSigned)}.{nameof(BlockSecureLootSigned.OnBlockActivated)}";

        // Create search pattern for GetItemStacksForFilter method call
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldloc_S, 5),  // itemValue
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.GetItemCount), [typeof(ItemValue)])),
        };

        // Create replacement instructions
        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldloc_S, 5),  // itemValue
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemCommon), nameof(ItemCommon.GetStorageItemCount))),
            new CodeInstruction(OpCodes.Add),
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 2,
            IsInsertMode = true,
            MaxPatches = 1,
            MinimumSafetyOffset = 2,
            ExtraLogging = false,
        };

        var response = ILPatchEngine.ApplyPatches(request);
        return response.BestInstructions(request);
    }
}