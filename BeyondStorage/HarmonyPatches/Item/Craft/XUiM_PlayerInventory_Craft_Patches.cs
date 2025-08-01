using System.Collections.Generic;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Harmony;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;

namespace BeyondStorage.HarmonyPatches.Item;

[HarmonyPatch(typeof(XUiM_PlayerInventory))]
public class XUiMPlayerInventoryCraftPatches
{
    // Used for:
    //          Item Crafting (has items only, does not handle remove)
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(XUiM_PlayerInventory.HasItems))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> XUiM_PlayerInventory_HasItems_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodName = $"{typeof(XUiM_PlayerInventory)}.{nameof(XUiM_PlayerInventory.HasItems)}";
        ModLogger.Info($"Transpiling {targetMethodName}");

        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ret)
        };

        // Create replacement instructions (insert before the found pattern)
        var replacementInstructions = new List<CodeInstruction>
        {
            // _itemStacks
            new CodeInstruction(OpCodes.Ldarg_1),
            // index
            new CodeInstruction(OpCodes.Ldloc_0),
            // num
            new CodeInstruction(OpCodes.Ldloc_1),
            // ItemCraft.ItemCraft_GetRemainingItemCount(_itemStacks, index, num)
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemCraft), nameof(ItemCraft.ItemCraft_GetRemainingItemCount))),
            // ldc.i4.0 (preserve original instruction with labels)
            new CodeInstruction(OpCodes.Ldc_I4_0),
            // ble.s <Label> (preserve original instruction with labels)
            new CodeInstruction(OpCodes.Ble_S, null) // The actual label will be preserved by the patch method
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodName,
            ReplacementOffset = 0,     // Insert at the match position
            IsInsertMode = true,       // Insert new instructions before the pattern
            MaxPatches = 1,
            MinimumSafetyOffset = 0,   // No special safety requirements
            ExtraLogging = false
        };

        var patchResult = ILPatchEngine.ApplyPatches(request);

        if (patchResult.IsPatched)
        {
            // -  1. Need to move this branch fixup code to the patch method
            // ✔️ 2. Record the original index of the patch as well as the new index of the patch in the PatchResult
            // -  3. use request.NewInstructions.GetRange();
            // -  4. Remove all this extra logging

            var newLabelIndex = request.NewInstructions.FindIndex(instr => instr.opcode == OpCodes.Ble_S && instr.labels.Count == 0);
            if (newLabelIndex >= 0)
            {
                var oldLabelIndex = patchResult.OriginalPositions[patchResult.Count - 1] - 1;

                var oldInstruction = request.OriginalInstructions[oldLabelIndex];
                var oldLabels = oldInstruction.labels;
                if (request.ExtraLogging)
                {
                    ModLogger.DebugLog($"{targetMethodName} found label instruction {oldInstruction.opcode} at new index {newLabelIndex} replacing with {oldLabels.Count} old labels");
                }

                request.NewInstructions[newLabelIndex] = oldInstruction.Clone();
            }
            else
            {
                // Could not find the label instruction, log an error
                ModLogger.Error($"{targetMethodName} patch failed: Could not find the label instruction for the branch.");
                return originalInstructions; // Return original instructions if patch failed
            }
        }

        return patchResult.BestInstructions(request);
    }
}