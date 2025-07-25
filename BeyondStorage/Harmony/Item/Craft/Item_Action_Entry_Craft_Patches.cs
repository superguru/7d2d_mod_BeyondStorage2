﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BeyondStorage.Scripts.ContainerLogic.Item;
using BeyondStorage.Scripts.Utils;
using HarmonyLib;

namespace BeyondStorage.Item.Action;

[HarmonyPatch(typeof(ItemActionEntryCraft))]
public class ItemActionEntryCraftPatches
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionEntryCraft.HasItems))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiC_ItemStackGrid_HandleSlotChangedEvent_Patch(IEnumerable<CodeInstruction> instructions)
    {
        var targetMethodString = $"{typeof(ItemActionEntryCraft)}.{nameof(ItemActionEntryCraft.HasItems)}";

        // Create search pattern for GetAllItemStacks method call
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.GetAllItemStacks)))
        };

        // Create replacement instructions
        var replacementInstructions = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Nop),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemCraft), nameof(ItemCraft.ItemCraft_GetAllAvailableItemStacksFromXui))),
            new CodeInstruction(OpCodes.Stloc_1)
        };

        var patchRequest = new PatchUtil.PatchRequest
        {
            OriginalInstructions = instructions.ToList(), // Convert to List for compatibility
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = -2,   // Start replacement 2 instructions before the match
            IsInsertMode = false,     // Overwrite existing instructions
            MaxPatches = 1,
            MinimumSafetyOffset = 2,  // Ensure we have at least 2 instructions before the match
            ExtraLogging = false      
        };

        var response = PatchUtil.ApplyPatches(patchRequest);
        return response.BestInstructions(patchRequest);
    }
}