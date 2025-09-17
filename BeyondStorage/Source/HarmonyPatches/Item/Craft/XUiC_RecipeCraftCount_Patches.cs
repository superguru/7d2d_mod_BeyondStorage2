using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Harmony;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Item;

[HarmonyPatch(typeof(XUiC_RecipeCraftCount))]
internal static class XUiCRecipeCraftCountPatches
{
    // Used for:
    //          Item Crafting (gets max craftable amount)
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(XUiC_RecipeCraftCount.calcMaxCraftable))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiC_RecipeCraftCount_calcMaxCraftable_Patch(
        IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(XUiC_RecipeCraftCount)}.{nameof(XUiC_RecipeCraftCount.calcMaxCraftable)}";
        var instructionsList = instructions as List<CodeInstruction> ?? instructions?.ToList() ?? new List<CodeInstruction>();

        // Create search pattern to find: callvirt XUiM_PlayerInventory.GetAllItemStacks
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.GetAllItemStacks)))
        };

        // Create replacement instructions that will be inserted after the GetAllItemStacks call
        var replacementInstructions = new List<CodeInstruction>
        {
            // ItemCraft.ItemCraft_MaxGetAllStorageStacks(result_from_GetAllItemStacks)
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemCraft), nameof(ItemCraft.ItemCraft_MaxGetAllStorageStacks)))
        };

        // Create the patch request
        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = instructionsList,
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 1, // Insert after the GetAllItemStacks call
            IsInsertMode = true, // We want to insert, not overwrite
            MaxPatches = 1, // Only patch the first occurrence
            MinimumSafetyOffset = 0, // No special context requirements
            ExtraLogging = false // Enable detailed logging for debugging
        };

        var response = ILPatchEngine.ApplyPatches(request);
        return response.BestInstructions(request);
    }
}