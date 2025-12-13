using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Harmony;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Item;

[HarmonyPatch(typeof(ItemActionEntryCraft))]
internal static class ItemActionEntryCraftPatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionEntryCraft.hasItems))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionEntryCraft_HasItems_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodString = $"{typeof(ItemActionEntryCraft)}.{nameof(ItemActionEntryCraft.hasItems)}";

        // Create search pattern for GetItemStacksForFilter method call
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.GetAllItemStacks)))
        };

        // Create replacement instructions
        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Nop),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemCommon), nameof(ItemCommon.ItemCommon_GetAllAvailableItemStacksFromXui))),
            new CodeInstruction(OpCodes.Stloc_1)
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = -2,   // Start replacement 2 instructions before the match
            IsInsertMode = false,     // Overwrite existing instructions
            MaxPatches = 1,
            MinimumSafetyOffset = 2,  // Ensure we have at least 2 instructions before the match
            ExtraLogging = false
        };

        var response = ILPatchEngine.ApplyPatches(request);
        return response.BestInstructions(request);
    }
}