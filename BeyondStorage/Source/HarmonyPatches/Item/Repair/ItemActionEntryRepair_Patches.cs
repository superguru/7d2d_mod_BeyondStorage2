using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BeyondStorage.Scripts.Game.Item;
using BeyondStorage.Scripts.Harmony;
using BeyondStorage.Scripts.Infrastructure;
using HarmonyLib;
using XMLData.Item;

namespace BeyondStorage.HarmonyPatches.Item;

[HarmonyPatch(typeof(ItemActionEntryRepair))]
internal static class ItemActionEntryRepairPatches
{
    // Used For:
    //      Item Repair (Allows Repair)
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionEntryRepair.OnActivated))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionEntryRepair_OnActivated_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodString = $"{typeof(ItemActionEntryRepair)}.{nameof(ItemActionEntryRepair.OnActivated)}";

        // Find the pattern that starts with Ldloc_1 and the subsequent instructions we need to clone
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Ldloc_1),        // playerInventory
            new CodeInstruction(OpCodes.Ldloc_S, 6),     // itemClass
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ItemData), nameof(ItemData.Id))),     // get_Id()
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(ItemValue), [typeof(int), typeof(bool)])), // new ItemValue(itemClass.Id, false)
        };

        // Create replacement instructions to add storage count
        var replacementInstructions = new List<CodeInstruction>
        {
            // Load the ItemValue that was used for GetItemCount (reconstruct it)
            new CodeInstruction(OpCodes.Ldloc_S, 6),     // itemClass
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ItemData), nameof(ItemData.Id))),     // get_Id()
            new CodeInstruction(OpCodes.Ldc_I4_0),       // false
            new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(ItemValue), [typeof(int), typeof(bool)])), // new ItemValue(itemClass.Id, false)
            
            // Call our storage method and add to existing count
            new CodeInstruction(OpCodes.Ldloc_S, 7),    // int b
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemRepair), nameof(ItemRepair.ItemRepairOnActivatedGetItemCount))),
            new CodeInstruction(OpCodes.Ldloc_S, 7),    // int b
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Mathf), nameof(UnityEngine.Mathf.Min), [typeof(int), typeof(int)])),
            new CodeInstruction(OpCodes.Stloc_S, 8),   // int _count
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 9,
            IsInsertMode = true,
            MaxPatches = 1,
            MinimumSafetyOffset = 2,
            ExtraLogging = false
        };

        var response = ILPatchEngine.ApplyPatches(request);
        return response.BestInstructions(request);
    }

    // Used For:
    //      Item Repair (Button Enabled)
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(ItemActionEntryRepair.RefreshEnabled))]
#if DEBUG
    [HarmonyDebug]
#endif
    private static IEnumerable<CodeInstruction> ItemActionEntryRepair_RefreshEnabled_Patch(IEnumerable<CodeInstruction> originalInstructions)
    {
        var targetMethodString = $"{typeof(ItemActionEntryRepair)}.{nameof(ItemActionEntryRepair.RefreshEnabled)}";

        // Create search pattern to find the GetItemCount call
        var searchPattern = new List<CodeInstruction>
        {
            new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.GetItemCount), [typeof(ItemValue)])),
            new CodeInstruction(OpCodes.Ldloc_S, 6),  // int b
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Mathf), nameof(UnityEngine.Mathf.Min), [typeof(int), typeof(int)])),
            new CodeInstruction(OpCodes.Ldloc_S, 5),      // itemClass2
        };

        // Create replacement instructions to add storage count
        var replacementInstructions = new List<CodeInstruction>
        {
            // Load the ItemValue that was used for GetItemCount (reconstruct it)
            new CodeInstruction(OpCodes.Ldloc_S, 5),      // itemClass2
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ItemClass), nameof(ItemClass.Id))),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(ItemValue), [typeof(int), typeof(bool)])), // new ItemValue(itemClass.Id, false)
            
            // Call our storage method and add to existing count
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ItemRepair), nameof(ItemRepair.ItemRepairRefreshGetItemCount))),
            new CodeInstruction(OpCodes.Ldloc_S, 6),  // int b
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Mathf), nameof(UnityEngine.Mathf.Min), [typeof(int), typeof(int)])),
            new CodeInstruction(OpCodes.Ldloc_S, 5),  // itemClass2
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ItemClass), nameof(ItemClass.RepairAmount))),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(DataItem<int>), nameof(DataItem<int>.Value))),
            new CodeInstruction(OpCodes.Mul),
            new CodeInstruction(OpCodes.Ldc_I4_0),
        };

        var request = new ILPatchEngine.PatchRequest
        {
            OriginalInstructions = [.. originalInstructions],
            SearchPattern = searchPattern,
            ReplacementInstructions = replacementInstructions,
            TargetMethodName = targetMethodString,
            ReplacementOffset = 9,
            IsInsertMode = true,
            MaxPatches = 1,
            MinimumSafetyOffset = 5,
            ExtraLogging = false
        };

        // Find the Ret instruction to get or create an end label
        var endInstruction = request.OriginalInstructions.LastOrDefault(instr => instr.opcode == OpCodes.Ret);
        
        Label endLabel;
        if (endInstruction != null && endInstruction.labels.Count > 0)
        {
            // Use existing label if available
            endLabel = endInstruction.labels[0];
        }
        else if (endInstruction != null)
        {
            // Create a new label if the Ret instruction exists but has no labels
            endLabel = new Label();
            endInstruction.labels.Add(endLabel);
        }
        else
        {
            // If no Ret instruction found, we can't create a proper branch
            var response = ILPatchEngine.ApplyPatches(request);
            return response.BestInstructions(request);
        }

        // Add the branch instruction with the label
        request.ReplacementInstructions.Add(new CodeInstruction(OpCodes.Bgt, endLabel));

        var patchResponse = ILPatchEngine.ApplyPatches(request);
        return patchResponse.BestInstructions(request);
    }
}